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

public class PromocionRepository : Repository<Promocion>, IPromocionRepository
{
    private static readonly ConcurrentDictionary<int, Promocion> CacheSesionFallback = new();
    private static int _ultimoIdFallback;

    public PromocionRepository(IDbContextFactory<FerreteriaDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<IReadOnlyList<Promocion>> ObtenerTodasConRelacionesAsync(CancellationToken ct = default)
    {
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var promociones = await db.Promociones
                .AsNoTracking()
                .Include(p => p.Producto)
                .Include(p => p.Categoria)
                .Include(p => p.Sucursal)
                .OrderByDescending(p => p.EstaActiva)
                .ThenByDescending(p => p.FechaFin)
                .ToListAsync(ct);

            try
            {
                var usosPorPromo = await db.DetalleVentas
                    .AsNoTracking()
                    .Where(d => d.IdPromocion != null)
                    .GroupBy(d => d.IdPromocion!.Value)
                    .Select(g => new
                    {
                        IdPromocion = g.Key,
                        Usos = g.Count(),
                        Monto = g.Sum(x => x.Subtotal)
                    })
                    .ToListAsync(ct);

                var mapaUsos = usosPorPromo.ToDictionary(x => x.IdPromocion);
                foreach (var promo in promociones)
                {
                    if (mapaUsos.TryGetValue(promo.Id, out var stat))
                    {
                        promo.CantidadUsos = stat.Usos;
                        promo.MontoTotalGenerado = stat.Monto;
                    }
                    CacheSesionFallback[promo.Id] = promo;
                }
            }
            catch
            {
                foreach (var promo in promociones)
                {
                    CacheSesionFallback[promo.Id] = promo;
                }
            }

            return promociones;
        }
        catch
        {
            return CacheSesionFallback.Values
                .OrderByDescending(p => p.EstaActiva)
                .ThenByDescending(p => p.FechaFin)
                .ToList();
        }
    }

    public async Task<IReadOnlyList<Promocion>> ObtenerVigentesAsync(
        int? idSucursal,
        DateTime? fechaUtc = null,
        CancellationToken ct = default)
    {
        var ahora = fechaUtc ?? DateTime.UtcNow;

        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var lista = await db.Promociones
                .AsNoTracking()
                .Include(p => p.Producto)
                .Include(p => p.Categoria)
                .Include(p => p.Sucursal)
                .Where(p => p.EstaActiva &&
                            p.FechaInicio <= ahora &&
                            p.FechaFin >= ahora &&
                            (p.IdSucursal == null || !idSucursal.HasValue || p.IdSucursal == idSucursal.Value))
                .OrderByDescending(p => p.FechaInicio)
                .ToListAsync(ct);

            foreach (var p in lista)
            {
                CacheSesionFallback[p.Id] = p;
            }

            return lista;
        }
        catch
        {
            return CacheSesionFallback.Values
                .Where(p => p.EstaActiva &&
                            p.FechaInicio <= ahora &&
                            p.FechaFin >= ahora &&
                            (p.IdSucursal == null || !idSucursal.HasValue || p.IdSucursal == idSucursal.Value))
                .OrderByDescending(p => p.FechaInicio)
                .ToList();
        }
    }

    public async Task<Promocion> GuardarPromocionAsync(Promocion promocion, CancellationToken ct = default)
    {
        if (promocion.FechaInicio.Kind != DateTimeKind.Utc)
            promocion.FechaInicio = promocion.FechaInicio.ToUniversalTime();

        if (promocion.FechaFin.Kind != DateTimeKind.Utc)
            promocion.FechaFin = promocion.FechaFin.ToUniversalTime();

        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);

            // Desacoplar navegaciones antes de guardar para que EF Core use solo las FKs
            var prodRef = promocion.Producto;
            var catRef = promocion.Categoria;
            var sucRef = promocion.Sucursal;
            promocion.Producto = null;
            promocion.Categoria = null;
            promocion.Sucursal = null;

            if (promocion.Id > 0)
            {
                db.Promociones.Update(promocion);
            }
            else
            {
                db.Promociones.Add(promocion);
            }

            await db.SaveChangesAsync(ct);

            promocion.Producto = prodRef;
            promocion.Categoria = catRef;
            promocion.Sucursal = sucRef;
            CacheSesionFallback[promocion.Id] = promocion;
            return promocion;
        }
        catch
        {
            if (promocion.Id <= 0)
            {
                promocion.Id = Interlocked.Increment(ref _ultimoIdFallback);
            }

            CacheSesionFallback[promocion.Id] = promocion;
            return promocion;
        }
    }

    public async Task CambiarEstadoActivaAsync(int idPromocion, bool estaActiva, CancellationToken ct = default)
    {
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var entidad = await db.Promociones.FirstOrDefaultAsync(p => p.Id == idPromocion, ct);
            if (entidad is not null)
            {
                entidad.EstaActiva = estaActiva;
                await db.SaveChangesAsync(ct);
            }
        }
        catch
        {
            // Fallback en memoria si aún no se migraron las columnas nuevas
        }

        if (CacheSesionFallback.TryGetValue(idPromocion, out var local))
        {
            local.EstaActiva = estaActiva;
        }
    }

    public async Task<(decimal PorcentajeVentasConPromo, decimal TotalVentasConPromo, decimal TotalDescontado, IReadOnlyList<RendimientoCampanaItem> TopCampanas)>
        CalcularRendimientoPeriodoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
    {
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);

            var totalVentasPeriodo = await db.Ventas
                .AsNoTracking()
                .CountAsync(v => v.FechaHora >= desdeUtc && v.FechaHora < hastaUtc, ct);

            var detallesConPromo = await db.DetalleVentas
                .AsNoTracking()
                .Include(d => d.Promocion)
                .Include(d => d.Venta)
                .Where(d => d.Venta.FechaHora >= desdeUtc &&
                            d.Venta.FechaHora < hastaUtc &&
                            (d.IdPromocion != null || d.MontoDescuento > 0m))
                .ToListAsync(ct);

            if (detallesConPromo.Count == 0)
            {
                return (0m, 0m, 0m, Array.Empty<RendimientoCampanaItem>());
            }

            var ventasUnicasConPromo = detallesConPromo
                .Select(d => d.IdVenta)
                .Distinct()
                .Count();

            var porcentajeAdopcion = totalVentasPeriodo > 0
                ? Math.Round((decimal)ventasUnicasConPromo / totalVentasPeriodo * 100m, 1)
                : 0m;

            var totalGenerado = detallesConPromo.Sum(d => d.Subtotal);
            var totalDescontado = detallesConPromo.Sum(d => d.MontoDescuento);

            var topCampanas = detallesConPromo
                .GroupBy(d => d.IdPromocion ?? 0)
                .Select(g =>
                {
                    var primera = g.First();
                    var nombre = primera.Promocion?.Nombre;
                    if (string.IsNullOrWhiteSpace(nombre) &&
                        primera.IdPromocion.HasValue &&
                        CacheSesionFallback.TryGetValue(primera.IdPromocion.Value, out var promoCache))
                    {
                        nombre = promoCache.Nombre;
                    }

                    return new RendimientoCampanaItem(
                        IdPromocion: g.Key,
                        NombrePromocion: string.IsNullOrWhiteSpace(nombre) ? $"Campaña #{g.Key}" : nombre!,
                        VecesAplicada: g.Count(),
                        TotalGenerado: g.Sum(x => x.Subtotal),
                        TotalDescontado: g.Sum(x => x.MontoDescuento));
                })
                .OrderByDescending(x => x.TotalGenerado)
                .ThenByDescending(x => x.VecesAplicada)
                .Take(3)
                .ToList();

            return (porcentajeAdopcion, totalGenerado, totalDescontado, topCampanas);
        }
        catch
        {
            return (0m, 0m, 0m, Array.Empty<RendimientoCampanaItem>());
        }
    }
}
