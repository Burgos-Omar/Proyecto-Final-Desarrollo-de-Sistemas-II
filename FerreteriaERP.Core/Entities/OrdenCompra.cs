using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerreteriaERP.Core.Entities;

public static class EstadosOrdenCompra
{
    public const string Pendiente = "Pendiente";
    public const string Recibida = "Recibida";
    public const string Cancelada = "Cancelada";
}

/// <summary>
/// Representa una Orden de Compra emitida hacia un proveedor para reabastecer stock en una sucursal (PASO 30).
/// </summary>
public class OrdenCompra
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty; // Ej. "OC-2026-0001"
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

    public int IdProveedor { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public int IdSucursal { get; set; }
    public Sucursal Sucursal { get; set; } = null!;

    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;

    /// <summary>"Pendiente", "Recibida", "Cancelada".</summary>
    public string Estado { get; set; } = EstadosOrdenCompra.Pendiente;

    public decimal TotalEstimado { get; set; }
    public string? Observaciones { get; set; }

    public ICollection<DetalleOrdenCompra> Detalles { get; set; } = new List<DetalleOrdenCompra>();

    // Propiedades auxiliares de presentación para el DataGrid
    [NotMapped]
    public string FechaEmisionTexto =>
        (FechaEmision.Kind == DateTimeKind.Utc ? FechaEmision.ToLocalTime() : FechaEmision)
        .ToString("dd/MM/yyyy HH:mm");

    [NotMapped]
    public string TotalEstimadoTexto => $"Bs. {TotalEstimado:N2}";

    [NotMapped]
    public string NombreProveedorTexto =>
        Proveedor?.NombreComercialMostrar ?? $"Proveedor #{IdProveedor}";

    [NotMapped]
    public bool EsPendiente =>
        string.Equals(Estado, EstadosOrdenCompra.Pendiente, StringComparison.OrdinalIgnoreCase);

    [NotMapped]
    public bool EsRecibida =>
        string.Equals(Estado, EstadosOrdenCompra.Recibida, StringComparison.OrdinalIgnoreCase);
}
