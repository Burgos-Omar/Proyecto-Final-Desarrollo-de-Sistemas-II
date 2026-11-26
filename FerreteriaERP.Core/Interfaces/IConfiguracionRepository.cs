using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces;

public interface IConfiguracionRepository : IRepository<ConfiguracionNegocio>
{
    Task<ConfiguracionNegocio> ObtenerConfiguracionAsync(CancellationToken ct = default);

    Task<ConfiguracionNegocio> GuardarQrPagoAsync(
        byte[]? imagen,
        string? banco,
        string? titular,
        CancellationToken ct = default);

    Task<ConfiguracionNegocio> GuardarDatosFiscalesAsync(
        string? nombreNegocio,
        string? nitNegocio,
        string? direccion = null,
        string? telefono = null,
        string? mensajePieRecibo = null,
        CancellationToken ct = default);

    Task<ConfiguracionNegocio> GuardarLogoAsync(
        byte[]? logoBytes,
        string? nombreNegocio = null,
        CancellationToken ct = default);
}
