using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories
{
    public class SucursalRepository : Repository<Sucursal>, ISucursalRepository
    {
        public SucursalRepository(IDbContextFactory<FerreteriaDbContext> contextFactory) : base(contextFactory)
        {
        }

        public async Task<Sucursal?> ObtenerCentralAsync(CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Sucursales.AsNoTracking().FirstOrDefaultAsync(s => s.EsCentral, ct);
        }
    }
}
