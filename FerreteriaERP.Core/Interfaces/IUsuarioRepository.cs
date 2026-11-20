using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        /// <summary>Busca por CI incluyendo Rol y Sucursal. Devuelve null si no existe.</summary>
        Task<Usuario?> ObtenerPorCiAsync(string ci, CancellationToken ct = default);

        /// <summary>Indica si ya existe un usuario con ese CI.</summary>
        Task<bool> ExisteCiAsync(string ci, CancellationToken ct = default);

        /// <summary>Usuarios con estado "Activo" de una sucursal.</summary>
        Task<IReadOnlyList<Usuario>> ObtenerActivosPorSucursalAsync(int idSucursal, CancellationToken ct = default);
    }
}
