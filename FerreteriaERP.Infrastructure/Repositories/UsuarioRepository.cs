using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories
{
    public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(IDbContextFactory<FerreteriaDbContext> contextFactory) : base(contextFactory)
        {
        }

        public async Task<Usuario?> ObtenerPorCiAsync(string ci, CancellationToken ct = default)
        {
            var ciLimpio = ci.Trim();
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Usuarios
                .AsNoTracking()
                .Include(u => u.Rol)
                .Include(u => u.Sucursal)
                .FirstOrDefaultAsync(u => u.Ci == ciLimpio, ct);
        }

        public Task<bool> ExisteCiAsync(string ci, CancellationToken ct = default)
        {
            var ciLimpio = ci.Trim();
            return ExisteAsync(u => u.Ci == ciLimpio, ct);
        }

        public async Task<IReadOnlyList<Usuario>> ObtenerActivosPorSucursalAsync(int idSucursal, CancellationToken ct = default)
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            return await db.Usuarios
                .AsNoTracking()
                .Include(u => u.Rol)
                .Where(u => u.IdSucursal == idSucursal && u.Estado == EstadosUsuario.Activo)
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync(ct);
        }
    }
}
