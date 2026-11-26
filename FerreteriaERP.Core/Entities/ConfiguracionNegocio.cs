using System;
using System.ComponentModel.DataAnnotations;

namespace FerreteriaERP.Core.Entities;

/// <summary>
/// Almacena la configuración comercial y fiscal del negocio, incluyendo Razón Social, NIT y QR bancario (PASO 31 / 32).
/// </summary>
public class ConfiguracionNegocio
{
    public int Id { get; set; }
    public string? NombreNegocio { get; set; } = "Ferretería ERP";

    [MaxLength(30)]
    public string? NitNegocio { get; set; }

    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? MensajePieRecibo { get; set; }

    public string? BancoQr { get; set; }
    public string? TitularQr { get; set; }
    public byte[]? QrPagoImagen { get; set; }
    public byte[]? LogoImagen { get; set; }
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}
