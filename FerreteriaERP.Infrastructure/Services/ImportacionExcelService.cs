using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de importación masiva desde archivos CSV (exportados desde Excel)
    /// compatible con CsvHelper v34+ (<see cref="CsvReaderOptions"/> y <see cref="CsvReader.FromFile"/>)
    /// y transacciones atómicas mediante <see cref="IDbContextFactory{TContext}"/>.
    /// </summary>
    public class ImportacionExcelService : IImportacionExcelService
    {
        private readonly IDbContextFactory<FerreteriaDbContext> _contextFactory;

        public ImportacionExcelService(IDbContextFactory<FerreteriaDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Crea las opciones de lectura para CsvHelper 34+:
        /// autodetección de separador (',', ';', '\t', '|'), lectura de cabeceras,
        /// limpieza de espacios y normalización de nombres de columna.
        /// </summary>
        private static CsvReaderOptions CrearOpcionesCsv()
        {
            return CsvReaderOptions.Default with
            {
                HasHeader = true,
                TrimWhitespace = true,
                TrimInsideQuotes = true,
                SkipEmptyLines = true,
                FormatProvider = CultureInfo.InvariantCulture,
                NormalizeHeaderName = name =>
                    name.Trim().Replace(" ", string.Empty).Replace("_", string.Empty)
            };
        }

        /// <inheritdoc />
        public async Task<int> ImportarProveedoresAsync(string filePath, CancellationToken ct = default)
        {
            ValidarArchivo(filePath);

            var filas = new List<ProveedorCsvRow>();

            await using (var csv = CsvReader.FromFile(filePath, CrearOpcionesCsv()))
            {
                while (await csv.ReadAsync(ct))
                {
                    filas.Add(new ProveedorCsvRow
                    {
                        NombreEmpresa = ObtenerCampo(csv, "NombreEmpresa"),
                        ContactoWhatsapp = ObtenerCampo(csv, "ContactoWhatsapp")
                    });
                }
            }

            if (filas.Count == 0)
                return 0;

            await using var db = await _contextFactory.CreateDbContextAsync(ct);
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            var nombresExistentes = (await db.Proveedores
                    .AsNoTracking()
                    .Select(p => p.NombreEmpresa)
                    .ToListAsync(ct))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var insertados = 0;

            foreach (var fila in filas)
            {
                var nombreEmpresa = fila.NombreEmpresa?.Trim();
                if (string.IsNullOrWhiteSpace(nombreEmpresa))
                    continue;

                // Omite si ya existe en la BD o si viene repetido dentro del mismo CSV
                if (!nombresExistentes.Add(nombreEmpresa))
                    continue;

                var contacto = string.IsNullOrWhiteSpace(fila.ContactoWhatsapp)
                    ? null
                    : fila.ContactoWhatsapp.Trim();

                db.Proveedores.Add(new Proveedor
                {
                    NombreEmpresa = nombreEmpresa,
                    ContactoWhatsapp = contacto
                });

                insertados++;
            }

            if (insertados > 0)
            {
                await db.SaveChangesAsync(ct);
            }

            await transaccion.CommitAsync(ct);
            return insertados;
        }

        /// <inheritdoc />
        public async Task<int> ImportarProductosAsync(string filePath, int idSucursalActual, CancellationToken ct = default)
        {
            ValidarArchivo(filePath);

            var filas = new List<ProductoCsvRow>();

            await using (var csv = CsvReader.FromFile(filePath, CrearOpcionesCsv()))
            {
                while (await csv.ReadAsync(ct))
                {
                    filas.Add(new ProductoCsvRow
                    {
                        CodigoBarras = ObtenerCampo(csv, "CodigoBarras"),
                        Nombre = ObtenerCampo(csv, "Nombre"),
                        Categoria = ObtenerCampo(csv, "Categoria"),
                        Proveedor = ObtenerCampo(csv, "Proveedor"),
                        PrecioCompra = ObtenerCampo(csv, "PrecioCompra"),
                        PrecioVenta = ObtenerCampo(csv, "PrecioVenta"),
                        StockInicial = ObtenerCampo(csv, "StockInicial"),
                        StockMinimo = ObtenerCampo(csv, "StockMinimo")
                    });
                }
            }

            if (filas.Count == 0)
                return 0;

            await using var db = await _contextFactory.CreateDbContextAsync(ct);
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            // 1. Cargamos las Categorías y Proveedores existentes en diccionarios insensibles a mayúsculas/minúsculas
            var categoriasPorNombre = (await db.Categorias.ToListAsync(ct))
                .GroupBy(c => c.Nombre.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var proveedoresPorNombre = (await db.Proveedores.ToListAsync(ct))
                .GroupBy(p => p.NombreEmpresa.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var huboNuevosCatalogos = false;

            // 2. Lógica de Negocio Crítica: Si la Categoría o el Proveedor no existen en la BD, los creamos al vuelo
            foreach (var fila in filas)
            {
                if (string.IsNullOrWhiteSpace(fila.Nombre))
                    continue;

                var nombreCategoria = string.IsNullOrWhiteSpace(fila.Categoria)
                    ? "General"
                    : fila.Categoria.Trim();

                if (!categoriasPorNombre.ContainsKey(nombreCategoria))
                {
                    var nuevaCategoria = new Categoria { Nombre = nombreCategoria };
                    db.Categorias.Add(nuevaCategoria);
                    categoriasPorNombre[nombreCategoria] = nuevaCategoria;
                    huboNuevosCatalogos = true;
                }

                var nombreProveedor = string.IsNullOrWhiteSpace(fila.Proveedor)
                    ? "Proveedor General"
                    : fila.Proveedor.Trim();

                if (!proveedoresPorNombre.ContainsKey(nombreProveedor))
                {
                    var nuevoProveedor = new Proveedor { NombreEmpresa = nombreProveedor };
                    db.Proveedores.Add(nuevoProveedor);
                    proveedoresPorNombre[nombreProveedor] = nuevoProveedor;
                    huboNuevosCatalogos = true;
                }
            }

            if (huboNuevosCatalogos)
            {
                await db.SaveChangesAsync(ct);
            }

            // 3. Cargamos productos existentes por código de barras para actualizar o insertar dentro de la misma transacción
            var productosPorCodigo = (await db.Productos
                    .Include(p => p.Stocks)
                    .Where(p => p.CodigoBarras != null)
                    .ToListAsync(ct))
                .GroupBy(p => p.CodigoBarras!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var procesados = 0;

            foreach (var fila in filas)
            {
                var nombreProducto = fila.Nombre?.Trim();
                if (string.IsNullOrWhiteSpace(nombreProducto))
                    continue;

                var codigoBarras = string.IsNullOrWhiteSpace(fila.CodigoBarras)
                    ? null
                    : fila.CodigoBarras.Trim();

                var nombreCategoria = string.IsNullOrWhiteSpace(fila.Categoria)
                    ? "General"
                    : fila.Categoria.Trim();

                var nombreProveedor = string.IsNullOrWhiteSpace(fila.Proveedor)
                    ? "Proveedor General"
                    : fila.Proveedor.Trim();

                var categoria = categoriasPorNombre[nombreCategoria];
                var proveedor = proveedoresPorNombre[nombreProveedor];

                var precioCompra = ParsearDecimal(fila.PrecioCompra);
                var precioVenta = ParsearDecimal(fila.PrecioVenta);
                var stockInicial = Math.Max(0, ParsearEntero(fila.StockInicial, 0));
                var stockMinimo = Math.Max(0, ParsearEntero(fila.StockMinimo, 5));

                if (!string.IsNullOrEmpty(codigoBarras) &&
                    productosPorCodigo.TryGetValue(codigoBarras, out var productoExistente))
                {
                    productoExistente.Nombre = nombreProducto;
                    productoExistente.PrecioCompra = precioCompra;
                    productoExistente.PrecioVenta = precioVenta;
                    productoExistente.StockMinimo = stockMinimo;
                    productoExistente.IdCategoria = categoria.Id;
                    productoExistente.IdProveedor = proveedor.Id;

                    var stockExistente = productoExistente.Stocks
                        .FirstOrDefault(s => s.IdSucursal == idSucursalActual);

                    if (stockExistente is null)
                    {
                        var nuevoStock = new StockSucursal
                        {
                            Producto = productoExistente,
                            IdSucursal = idSucursalActual,
                            CantidadActual = stockInicial
                        };
                        productoExistente.Stocks.Add(nuevoStock);
                        db.StockSucursales.Add(nuevoStock);
                    }
                    else
                    {
                        stockExistente.CantidadActual = stockInicial;
                    }
                }
                else
                {
                    var nuevoProducto = new Producto
                    {
                        CodigoBarras = codigoBarras,
                        Nombre = nombreProducto,
                        PrecioCompra = precioCompra,
                        PrecioVenta = precioVenta,
                        StockMinimo = stockMinimo,
                        IdCategoria = categoria.Id,
                        IdProveedor = proveedor.Id
                    };

                    db.Productos.Add(nuevoProducto);

                    var stockSucursal = new StockSucursal
                    {
                        Producto = nuevoProducto,
                        IdSucursal = idSucursalActual,
                        CantidadActual = stockInicial
                    };

                    nuevoProducto.Stocks.Add(stockSucursal);
                    db.StockSucursales.Add(stockSucursal);

                    if (!string.IsNullOrEmpty(codigoBarras))
                    {
                        productosPorCodigo[codigoBarras] = nuevoProducto;
                    }
                }

                procesados++;
            }

            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);

            return procesados;
        }

        private static string? ObtenerCampo(CsvReader csv, string nombreColumna)
        {
            if (!csv.TryGetFieldIndex(nombreColumna, out var index))
                return null;

            var valor = csv.GetString(index);
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static void ValidarArchivo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("La ruta del archivo CSV no puede estar vacía.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("No se encontró el archivo CSV seleccionado.", filePath);
        }

        private static decimal ParsearDecimal(string? valorTexto)
        {
            if (string.IsNullOrWhiteSpace(valorTexto))
                return 0m;

            var limpio = valorTexto.Trim().Replace(',', '.');
            return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out var resultado) && resultado >= 0m
                ? resultado
                : 0m;
        }

        private static int ParsearEntero(string? valorTexto, int valorPorDefecto)
        {
            if (string.IsNullOrWhiteSpace(valorTexto))
                return valorPorDefecto;

            if (int.TryParse(valorTexto.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var entero))
                return entero;

            if (decimal.TryParse(valorTexto.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var dec))
                return (int)Math.Round(dec);

            return valorPorDefecto;
        }

        private sealed class ProveedorCsvRow
        {
            public string? NombreEmpresa { get; set; }
            public string? ContactoWhatsapp { get; set; }
        }

        private sealed class ProductoCsvRow
        {
            public string? CodigoBarras { get; set; }
            public string? Nombre { get; set; }
            public string? Categoria { get; set; }
            public string? Proveedor { get; set; }
            public string? PrecioCompra { get; set; }
            public string? PrecioVenta { get; set; }
            public string? StockInicial { get; set; }
            public string? StockMinimo { get; set; }
        }
    }
}
