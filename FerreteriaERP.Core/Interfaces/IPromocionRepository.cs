using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces;

public record RendimientoCampanaItem(
    int IdPromocion,
    string NombrePromocion,
    int VecesAplicada,
    decimal TotalGenerado,
    decimal TotalDescontado)
{
    public string VecesAplicadaTexto => VecesAplicada == 1
        ? "1 aplicación"
        : $"{VecesAplicada} aplicaciones";

    public string TotalGeneradoTexto => $"Bs. {TotalGenerado:N2}";
}

public interface IPromocionRepository : IRepository<Promocion>
{
    Task<IReadOnlyList<Promocion>> ObtenerTodasConRelacionesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Promocion>> ObtenerVigentesAsync(
        int? idSucursal,
        DateTime? fechaUtc = null,
        CancellationToken ct = default);

    Task<Promocion> GuardarPromocionAsync(Promocion promocion, CancellationToken ct = default);

    Task CambiarEstadoActivaAsync(int idPromocion, bool estaActiva, CancellationToken ct = default);

    Task<(decimal PorcentajeVentasConPromo, decimal TotalVentasConPromo, decimal TotalDescontado, IReadOnlyList<RendimientoCampanaItem> TopCampanas)>
        CalcularRendimientoPeriodoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);
}
