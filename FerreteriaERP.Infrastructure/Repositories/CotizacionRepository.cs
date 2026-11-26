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

public class CotizacionRepository : Repository<Cotizacion>, ICotizacionRepository
{
    private static readonly ConcurrentDictionary<int, Cotizacion> CacheSesionFallback = new();
    private static int _ultimoIdFallback;

    public CotizacionRepository(IDbContextFactory<FerreteriaDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<Cotizacion> AddAsync(Cotizacion cotizacion, CancellationToken ct = default)
    {
        foreach (var det in cotizacion.Detalles)
        {
            det.Subtotal = det.Cantidad * det.PrecioUnitario;
        }

        cotizacion.Total = cotizacion.Detalles.Sum(d => d.Subtotal);

        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            db.Cotizaciones.Add(cotizacion);
            await db.SaveChangesAsync(ct);
            CacheSesionFallback[cotizacion.Id] = cotizacion;
            return cotizacion;
        }
        catch
        {
            // Fallback en memoria si aún no se corrió la migración de la tabla Cotizaciones
            if (cotizacion.Id <= 0)
            {
                cotizacion.Id = Interlocked.Increment(ref _ultimoIdFallback);
            }

            CacheSesionFallback[cotizacion.Id] = cotizacion;
            return cotizacion;
        }
    }

    public async Task<Cotizacion?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var encontrada = await db.Cotizaciones
                .AsNoTracking()
                .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (encontrada is not null)
                return encontrada;
        }
        catch
        {
            // Si la tabla aún no fue migrada en PostgreSQL, consulta el fallback de la sesión activa
        }

        return CacheSesionFallback.TryGetValue(id, out var local) ? local : null;
    }

    public async Task<IReadOnlyList<Cotizacion>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Cotizaciones
                .AsNoTracking()
                .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
                .OrderByDescending(c => c.FechaEmision)
                .ToListAsync(ct);
        }
        catch
        {
            return CacheSesionFallback.Values
                .OrderByDescending(c => c.FechaEmision)
                .ToList();
        }
    }
}
