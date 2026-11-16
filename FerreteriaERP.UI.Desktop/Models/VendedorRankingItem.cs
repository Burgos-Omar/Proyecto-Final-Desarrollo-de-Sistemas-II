using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.UI.Desktop.Models;

/// <summary>
/// Modelo de presentación para la Tier List / Ranking de Ventas de los vendedores.
/// </summary>
public class VendedorRankingItem
{
    public int Posicion { get; init; }

    public Empleado Empleado { get; init; } = null!;

    public int CantidadVentas { get; init; }

    public decimal MontoTotalVendido { get; init; }

    public string PosicionTexto => $"#{Posicion}";

    public string NombreCompleto => Empleado.NombreCompleto;

    public string Puesto => Empleado.Puesto;

    public string? ImagenUrl => Empleado.ImagenUrl;

    public string Iniciales => Empleado.Iniciales;

    public string SucursalNombre => Empleado.SucursalNombreMostrar;

    public string CantidadVentasTexto => CantidadVentas == 1
        ? "1 venta concretada"
        : $"{CantidadVentas} ventas concretadas";

    public string MontoTotalFormateado => $"Bs. {MontoTotalVendido:N2}";

    public bool EsTop1 => Posicion == 1;
    public bool EsTop2 => Posicion == 2;
    public bool EsTop3 => Posicion == 3;
    public bool EsPodio => Posicion <= 3;
    public bool NoEsPodio => Posicion > 3;

    public string TierBadgeTexto => Posicion switch
    {
        1 => "TIER S • ORO",
        2 => "TIER A • PLATA",
        3 => "TIER B • BRONCE",
        _ => $"PUESTO #{Posicion}"
    };
}

