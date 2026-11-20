using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Exceptions;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories
{
    public class ProductoRepository : Repository<Producto>, IProductoRepository
    {
        public ProductoRepository(IDbContextFactory<FerreteriaDbContext> contextFactory) : base(contextFactory)
        {
        }

        public async Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default)
        {
            var codigo = codigoBarras.Trim();
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.CodigoBarras == codigo, ct);
        }

        public async Task<IReadOnlyList<Producto>> BuscarPorNombreAsync(
            string texto, int maximo = 50, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Productos
                .AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Nombre, $"%{texto.Trim()}%"))
                .OrderBy(p => p.Nombre)
                .Take(maximo)
                .ToListAsync(ct);
        }

        public async Task<int> ObtenerStockAsync(int idProducto, int idSucursal, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.StockSucursales
                .Where(s => s.IdProducto == idProducto && s.IdSucursal == idSucursal)
                .Select(s => s.CantidadActual)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<StockSucursal>> ObtenerStockBajoAsync(int idSucursal, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.StockSucursales
                .AsNoTracking()
                .Include(s => s.Producto)
                .Where(s => s.IdSucursal == idSucursal && s.CantidadActual <= s.Producto.StockMinimo)
                .OrderBy(s => s.Producto.Nombre)
                .ToListAsync(ct);
        }

        public async Task AjustarStockAsync(int idProducto, int idSucursal, int delta, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);

            // UPDATE atómico en SQL: evita condiciones de carrera si dos sucursales venden el mismo producto a la vez.
            var filas = await db.StockSucursales
                .Where(s => s.IdProducto == idProducto && s.IdSucursal == idSucursal
                            && s.CantidadActual + delta >= 0)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CantidadActual, x => x.CantidadActual + delta), ct);

            if (filas > 0) return;

            var existeRegistro = await db.StockSucursales
                .AnyAsync(s => s.IdProducto == idProducto && s.IdSucursal == idSucursal, ct);

            // Hay registro pero el resultado sería negativo, o se intenta sacar de un producto sin stock.
            if (existeRegistro || delta < 0)
                throw new StockInsuficienteException(idProducto, idSucursal, Math.Abs(delta));

            // Primer ingreso de este producto en esta sucursal.
            db.StockSucursales.Add(new StockSucursal
            {
                IdProducto = idProducto,
                IdSucursal = idSucursal,
                CantidadActual = delta
            });
            await db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<Producto>> ObtenerCatalogoConStockSucursalAsync(
            int idSucursal, string? filtro = null, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);

            IQueryable<Producto> query = db.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .Include(p => p.Stocks.Where(s => s.IdSucursal == idSucursal));

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var termino = $"%{filtro.Trim()}%";
                query = query.Where(p =>
                    (p.CodigoBarras != null && EF.Functions.ILike(p.CodigoBarras, termino)) ||
                    EF.Functions.ILike(p.Nombre, termino));
            }

            return await query
                .OrderBy(p => p.Nombre)
                .ToListAsync(ct);
        }

        public async Task<Producto> CrearConStockInicialAsync(
            Producto producto, int idSucursal, int stockInicial, CancellationToken ct = default)
        {
            if (stockInicial < 0)
                throw new InvalidOperationException("El stock inicial no puede ser negativo.");

            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            if (!string.IsNullOrWhiteSpace(producto.CodigoBarras))
            {
                var codigoLimpio = producto.CodigoBarras.Trim();
                var duplicado = await db.Productos.AnyAsync(p => p.CodigoBarras == codigoLimpio, ct);
                if (duplicado)
                    throw new InvalidOperationException($"Ya existe un producto con el código de barras '{codigoLimpio}'.");

                producto.CodigoBarras = codigoLimpio;
            }
            else
            {
                producto.CodigoBarras = null;
            }

            db.Productos.Add(producto);
            await db.SaveChangesAsync(ct); // Genera producto.Id

            db.StockSucursales.Add(new StockSucursal
            {
                IdProducto = producto.Id,
                IdSucursal = idSucursal,
                CantidadActual = stockInicial
            });
            await db.SaveChangesAsync(ct);

            await transaccion.CommitAsync(ct);
            return producto;
        }

        public async Task ActualizarConStockSucursalAsync(
            Producto producto, int idSucursal, int stockSucursal, CancellationToken ct = default)
        {
            if (stockSucursal < 0)
                throw new InvalidOperationException("El stock actual no puede ser negativo.");

            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            var codigoLimpio = string.IsNullOrWhiteSpace(producto.CodigoBarras)
                ? null
                : producto.CodigoBarras.Trim();

            if (codigoLimpio is not null)
            {
                var duplicado = await db.Productos
                    .AnyAsync(p => p.Id != producto.Id && p.CodigoBarras == codigoLimpio, ct);
                if (duplicado)
                    throw new InvalidOperationException($"Otro producto ya utiliza el código de barras '{codigoLimpio}'.");
            }

            var filasProducto = await db.Productos
                .Where(p => p.Id == producto.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.CodigoBarras, codigoLimpio)
                    .SetProperty(p => p.Nombre, producto.Nombre.Trim())
                    .SetProperty(p => p.ImagenUrl, producto.ImagenUrl)
                    .SetProperty(p => p.PrecioCompra, producto.PrecioCompra)
                    .SetProperty(p => p.PrecioVenta, producto.PrecioVenta)
                    .SetProperty(p => p.StockMinimo, producto.StockMinimo)
                    .SetProperty(p => p.IdCategoria, producto.IdCategoria)
                    .SetProperty(p => p.IdProveedor, producto.IdProveedor), ct);

            if (filasProducto == 0)
                throw new InvalidOperationException("El producto que intenta editar ya no existe en la base de datos.");

            var filasStock = await db.StockSucursales
                .Where(s => s.IdProducto == producto.Id && s.IdSucursal == idSucursal)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CantidadActual, stockSucursal), ct);

            if (filasStock == 0)
            {
                db.StockSucursales.Add(new StockSucursal
                {
                    IdProducto = producto.Id,
                    IdSucursal = idSucursal,
                    CantidadActual = stockSucursal
                });
                await db.SaveChangesAsync(ct);
            }

            await transaccion.CommitAsync(ct);
        }
    }
}
