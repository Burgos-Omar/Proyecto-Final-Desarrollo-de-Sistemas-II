using System;
using System.Collections.Generic;

namespace FerreteriaERP.Core.Entities;

/// <summary>
/// Representa una proforma o cotización emitida a un cliente sin afectar el stock ni los ingresos de caja.
/// </summary>
public class Cotizacion
{
    public int Id { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public int ValidezDias { get; set; } = 7;
    public string NombreCliente { get; set; } = "S/N";
    public string NitCliente { get; set; } = "0";
    public decimal Total { get; set; }

    public ICollection<DetalleCotizacion> Detalles { get; set; } = new List<DetalleCotizacion>();
}

/// <summary>
/// Ítem individual dentro de una <see cref="Cotizacion"/>.
/// </summary>
public class DetalleCotizacion
{
    public int Id { get; set; }

    public int IdCotizacion { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    // Trazabilidad opcional de promociones en proformas (PASO 29)
    public int? IdPromocion { get; set; }
    public Promocion? Promocion { get; set; }
    public decimal MontoDescuento { get; set; }
}
