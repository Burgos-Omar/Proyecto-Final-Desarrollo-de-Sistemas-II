using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces;

public interface IOrdenCompraRepository : IRepository<OrdenCompra>
{
    Task<string> GenerarSiguienteNumeroOrdenAsync(CancellationToken ct = default);

    Task<OrdenCompra> CrearOrdenCompraAsync(OrdenCompra orden, CancellationToken ct = default);

    Task MarcarComoRecibidaAsync(int idOrdenCompra, CancellationToken ct = default);

    Task<IReadOnlyList<OrdenCompra>> ObtenerHistorialAsync(int? idSucursal = null, CancellationToken ct = default);
}
