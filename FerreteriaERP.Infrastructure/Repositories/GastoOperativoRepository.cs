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

public class GastoOperativoRepository : Repository<GastoOperativo>, IGastoOperativoRepository
{
    private static readonly ConcurrentDictionary<int, GastoOperativo> CacheSesionFallback = new();
    private static int _ultimoIdFallback;

    public GastoOperativoRepository(IDbContextFactory<FerreteriaDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<GastoOperativo> AgregarGastoAsync(GastoOperativo gasto, CancellationToken ct = default)
    {
        if (gasto.Fecha.Kind != DateTimeKind.Utc)
        {
            gasto.Fecha = gasto.Fecha.ToUniversalTime();
        }

        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            db.GastosOperativos.Add(gasto);
            await db.SaveChangesAsync(ct);
            CacheSesionFallback[gasto.Id] = gasto;
            return gasto;
        }
        catch
        {
            // Fallback en memoria si la tabla gastos_operativos aún no fue migrada en PostgreSQL
            if (gasto.Id <= 0)
            {
                gasto.Id = Interlocked.Increment(ref _ultimoIdFallback);
            }

            CacheSesionFallback[gasto.Id] = gasto;
            return gasto;
        }
    }

    public async Task<IReadOnlyList<GastoOperativo>> ObtenerPorRangoAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? idSucursal = null,
        CancellationToken ct = default)
    {
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var query = db.GastosOperativos
                .AsNoTracking()
                .Where(g => g.Fecha >= desdeUtc && g.Fecha < hastaUtc);

            if (idSucursal.HasValue && idSucursal.Value > 0)
            {
                query = query.Where(g => g.IdSucursal == idSucursal.Value);
            }

            return await query
                .OrderByDescending(g => g.Fecha)
                .ThenByDescending(g => g.Id)
                .ToListAsync(ct);
        }
        catch
        {
            return CacheSesionFallback.Values
                .Where(g => g.Fecha >= desdeUtc && g.Fecha < hastaUtc &&
                            (!idSucursal.HasValue || idSucursal.Value <= 0 || g.IdSucursal == idSucursal.Value))
                .OrderByDescending(g => g.Fecha)
                .ToList();
        }
    }
}
