using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories
{
    public class EntregaRepository : Repository<Entrega>, IEntregaRepository
    {
        public EntregaRepository(IDbContextFactory<FerreteriaDbContext> contextFactory) : base(contextFactory)
        {
        }

        public async Task<IReadOnlyList<Entrega>> ObtenerPorEstadoAsync(
            string estado, int? idSucursal = null, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var consulta = db.Entregas
                .AsNoTracking()
                .Include(e => e.Venta)
                .Include(e => e.Chofer)
                .Where(e => e.EstadoEntrega == estado);

            if (idSucursal is int sucursal)
                consulta = consulta.Where(e => e.Venta.IdSucursal == sucursal);

            return await consulta.OrderBy(e => e.Id).ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Entrega>> ObtenerPendientesPorChoferAsync(int idChofer, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Entregas
                .AsNoTracking()
                .Include(e => e.Venta)
                .Where(e => e.IdChofer == idChofer && e.EstadoEntrega != EstadosEntrega.Entregado)
                .OrderBy(e => e.Id)
                .ToListAsync(ct);
        }

        public async Task<bool> CambiarEstadoAsync(int idEntrega, string nuevoEstado, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var filas = await db.Entregas
                .Where(e => e.Id == idEntrega)
                .ExecuteUpdateAsync(e => e.SetProperty(x => x.EstadoEntrega, nuevoEstado), ct);
            return filas > 0;
        }
    }
}
