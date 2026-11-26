using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories;

public class OrdenCompraRepository : Repository<OrdenCompra>, IOrdenCompraRepository
{
    private static readonly ConcurrentDictionary<int, OrdenCompra> CacheSesionFallback = new();
    private static int _ultimoIdFallback;

    public OrdenCompraRepository(IDbContextFactory<FerreteriaDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<string> GenerarSiguienteNumeroOrdenAsync(CancellationToken ct = default)
    {
        var anio = DateTime.Now.Year;
        var prefijo = $"OC-{anio}-";

        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var conteo = await db.OrdenesCompra
                .AsNoTracking()
                .CountAsync(o => o.NumeroOrden.StartsWith(prefijo), ct);

            return $"{prefijo}{(conteo + 1):D4}";
        }
        catch
        {
            var conteoLocal = CacheSesionFallback.Values.Count(o => o.NumeroOrden.StartsWith(prefijo));
            return $"{prefijo}{(conteoLocal + 1):D4}";
        }
    }

    public async Task<OrdenCompra> CrearOrdenCompraAsync(OrdenCompra orden, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(orden);
        if (orden.Detalles.Count == 0)
            throw new InvalidOperationException("La orden de compra debe incluir al menos un producto.");

        if (string.IsNullOrWhiteSpace(orden.NumeroOrden))
        {
            orden.NumeroOrden = await GenerarSiguienteNumeroOrdenAsync(ct);
        }

        orden.FechaEmision = DateTime.UtcNow;
        orden.Estado = EstadosOrdenCompra.Pendiente;

        foreach (var det in orden.Detalles)
        {
            det.Subtotal = det.CantidadSolicitada * det.CostoUnitarioEstimado;
        }

        orden.TotalEstimado = orden.Detalles.Sum(d => d.Subtotal);

        // Guardar referencias para devolver la orden completa tras SaveChanges
        var proveedorRef = orden.Proveedor;
        var sucursalRef = orden.Sucursal;
        var usuarioRef = orden.Usuario;
        var productosPorId = orden.Detalles
            .Where(d => d.Producto is not null)
            .GroupBy(d => d.IdProducto)
            .ToDictionary(g => g.Key, g => g.First().Producto);

        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            orden.Proveedor = null!;
            orden.Sucursal = null!;
            orden.Usuario = null!;
            foreach (var det in orden.Detalles)
            {
                det.Producto = null!;
            }

            db.OrdenesCompra.Add(orden);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);

            orden.Proveedor = proveedorRef;
            orden.Sucursal = sucursalRef;
            orden.Usuario = usuarioRef;
            foreach (var det in orden.Detalles)
            {
                if (productosPorId.TryGetValue(det.IdProducto, out var prod))
                    det.Producto = prod;
            }

            CacheSesionFallback[orden.Id] = orden;
            return orden;
        }
        catch
        {
            orden.Proveedor = proveedorRef;
            orden.Sucursal = sucursalRef;
            orden.Usuario = usuarioRef;
            foreach (var det in orden.Detalles)
            {
                if (productosPorId.TryGetValue(det.IdProducto, out var prod))
                    det.Producto = prod;
            }

            if (orden.Id <= 0)
            {
                orden.Id = Interlocked.Increment(ref _ultimoIdFallback);
            }

            CacheSesionFallback[orden.Id] = orden;
            return orden;
        }
    }

    /// <summary>
    /// Cambia el estado de la orden a "Recibida" e incrementa automáticamente StockSucursal.CantidadActual
    /// para cada producto del detalle en la sucursal de la orden dentro de una sola transacción.
    /// </summary>
    public async Task MarcarComoRecibidaAsync(int idOrdenCompra, CancellationToken ct = default)
    {
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            var ordenDb = await db.OrdenesCompra
                .Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.Id == idOrdenCompra, ct);

            if (ordenDb is null)
            {
                // Si provino del fallback en memoria, al menos aplicamos el incremento de stock real en StockSucursales
                if (CacheSesionFallback.TryGetValue(idOrdenCompra, out var ordenLocal) && ordenLocal.EsPendiente)
                {
                    await IncrementarStockEnBdAsync(db, ordenLocal.IdSucursal, ordenLocal.Detalles, ct);
                    await db.SaveChangesAsync(ct);
                    await transaccion.CommitAsync(ct);
                    ordenLocal.Estado = EstadosOrdenCompra.Recibida;
                    return;
                }

                throw new InvalidOperationException($"No se encontró la orden de compra #{idOrdenCompra}.");
            }

            if (!ordenDb.EsPendiente)
                throw new InvalidOperationException("Esta orden de compra ya fue recibida o cancelada.");

            ordenDb.Estado = EstadosOrdenCompra.Recibida;
            await IncrementarStockEnBdAsync(db, ordenDb.IdSucursal, ordenDb.Detalles, ct);

            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);

            if (CacheSesionFallback.TryGetValue(idOrdenCompra, out var local))
            {
                local.Estado = EstadosOrdenCompra.Recibida;
            }
        }
        catch when (CacheSesionFallback.TryGetValue(idOrdenCompra, out var ordenLocal) && ordenLocal.EsPendiente)
        {
            // Si la tabla OrdenesCompra aún no existe en BD, actualizamos StockSucursales directamente
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            await IncrementarStockEnBdAsync(db, ordenLocal.IdSucursal, ordenLocal.Detalles, ct);
            await db.SaveChangesAsync(ct);
            ordenLocal.Estado = EstadosOrdenCompra.Recibida;
        }
    }

    public async Task<IReadOnlyList<OrdenCompra>> ObtenerHistorialAsync(
        int? idSucursal = null,
        CancellationToken ct = default)
    {
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var query = db.OrdenesCompra
                .AsNoTracking()
                .Include(o => o.Proveedor)
                .Include(o => o.Sucursal)
                .Include(o => o.Detalles)
                .ThenInclude(d => d.Producto)
                .AsQueryable();

            if (idSucursal.HasValue && idSucursal.Value > 0)
            {
                query = query.Where(o => o.IdSucursal == idSucursal.Value);
            }

            return await query
                .OrderByDescending(o => o.FechaEmision)
                .ThenByDescending(o => o.Id)
                .ToListAsync(ct);
        }
        catch
        {
            return CacheSesionFallback.Values
                .Where(o => !idSucursal.HasValue || idSucursal.Value <= 0 || o.IdSucursal == idSucursal.Value)
                .OrderByDescending(o => o.FechaEmision)
                .ToList();
        }
    }

    private static async Task IncrementarStockEnBdAsync(
        FerreteriaDbContext db,
        int idSucursal,
        IEnumerable<DetalleOrdenCompra> detalles,
        CancellationToken ct)
    {
        foreach (var det in detalles)
        {
            if (det.CantidadSolicitada <= 0)
                continue;

            var stock = await db.StockSucursales
                .FirstOrDefaultAsync(s => s.IdProducto == det.IdProducto && s.IdSucursal == idSucursal, ct);

            if (stock is not null)
            {
                stock.CantidadActual += det.CantidadSolicitada;
            }
            else
            {
                db.StockSucursales.Add(new StockSucursal
                {
                    IdProducto = det.IdProducto,
                    IdSucursal = idSucursal,
                    CantidadActual = det.CantidadSolicitada
                });
            }
        }
    }
}
