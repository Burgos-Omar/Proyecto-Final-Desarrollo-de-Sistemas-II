using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces
{
    public interface IEntregaRepository : IRepository<Entrega>
    {
        /// <summary>Entregas en un estado dado (ver <c>EstadosEntrega</c>), opcionalmente filtradas por sucursal de la venta.</summary>
        Task<IReadOnlyList<Entrega>> ObtenerPorEstadoAsync(string estado, int? idSucursal = null, CancellationToken ct = default);

        /// <summary>Entregas asignadas a un chofer que aún no han sido entregadas.</summary>
        Task<IReadOnlyList<Entrega>> ObtenerPendientesPorChoferAsync(int idChofer, CancellationToken ct = default);

        /// <summary>Cambia el estado de una entrega. Devuelve false si la entrega no existe.</summary>
        Task<bool> CambiarEstadoAsync(int idEntrega, string nuevoEstado, CancellationToken ct = default);
    }
}
