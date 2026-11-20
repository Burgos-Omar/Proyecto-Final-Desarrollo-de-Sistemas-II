using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces
{
    public interface IClienteRepository : IRepository<Cliente>
    {
        /// <summary>Busca un cliente por CI. Devuelve null si no existe.</summary>
        Task<Cliente?> ObtenerPorCiAsync(string ci, CancellationToken ct = default);

        /// <summary>Búsqueda por nombre (sin distinguir mayúsculas), limitada a <paramref name="maximo"/> resultados.</summary>
        Task<IReadOnlyList<Cliente>> BuscarPorNombreAsync(string texto, int maximo = 50, CancellationToken ct = default);
    }
}
