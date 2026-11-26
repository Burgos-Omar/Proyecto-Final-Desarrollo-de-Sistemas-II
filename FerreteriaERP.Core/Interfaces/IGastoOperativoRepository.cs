using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces;

public interface IGastoOperativoRepository : IRepository<GastoOperativo>
{
    Task<GastoOperativo> AgregarGastoAsync(GastoOperativo gasto, CancellationToken ct = default);

    Task<IReadOnlyList<GastoOperativo>> ObtenerPorRangoAsync(
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? idSucursal = null,
        CancellationToken ct = default);
}
