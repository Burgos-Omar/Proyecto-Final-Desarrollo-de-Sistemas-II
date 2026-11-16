using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces
{
    public interface ISucursalRepository : IRepository<Sucursal>
    {
        /// <summary>Devuelve la sucursal marcada como Central (<c>EsCentral = true</c>).</summary>
        Task<Sucursal?> ObtenerCentralAsync(CancellationToken ct = default);
    }
}
