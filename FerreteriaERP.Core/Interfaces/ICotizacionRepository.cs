using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces;

public interface ICotizacionRepository : IRepository<Cotizacion>
{
    Task<Cotizacion> AddAsync(Cotizacion cotizacion, CancellationToken ct = default);
    Task<Cotizacion?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Cotizacion>> GetAllAsync(CancellationToken ct = default);
}
