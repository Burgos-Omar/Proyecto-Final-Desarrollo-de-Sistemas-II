using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories
{
    public class ClienteRepository : Repository<Cliente>, IClienteRepository
    {
        public ClienteRepository(IDbContextFactory<FerreteriaDbContext> contextFactory) : base(contextFactory)
        {
        }

        public async Task<Cliente?> ObtenerPorCiAsync(string ci, CancellationToken ct = default)
        {
            var ciLimpio = ci.Trim();
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Ci == ciLimpio, ct);
        }

        public async Task<IReadOnlyList<Cliente>> BuscarPorNombreAsync(
            string texto, int maximo = 50, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Clientes
                .AsNoTracking()
                .Where(c => EF.Functions.ILike(c.NombreCompleto, $"%{texto.Trim()}%"))
                .OrderBy(c => c.NombreCompleto)
                .Take(maximo)
                .ToListAsync(ct);
        }
    }
}
