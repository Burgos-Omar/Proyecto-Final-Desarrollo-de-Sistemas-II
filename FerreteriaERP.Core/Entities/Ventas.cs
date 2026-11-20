using System;
using System.Collections.Generic;

namespace FerreteriaERP.Core.Entities
{
    public class Cliente
    {
        public int Id { get; set; }
        public string? Ci { get; set; }
        public string NombreCompleto { get; set; } = null!;
        public string? CelularWhatsapp { get; set; }
        public int CantidadCompras { get; set; }

        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<Credito> Creditos { get; set; } = new List<Credito>();
    }

    public class Venta
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public decimal Total { get; set; }
        public string MetodoPago { get; set; } = null!; // Efectivo, QR, Credito
        public decimal MontoRecibido { get; set; }
        public decimal CambioDado { get; set; }

        public int IdUsuario { get; set; }
        public Usuario Usuario { get; set; } = null!; // El Cajero

        public int IdSucursal { get; set; }
        public Sucursal Sucursal { get; set; } = null!;

        public int? IdCliente { get; set; }
        public Cliente? Cliente { get; set; }

        public int? IdEmpleado { get; set; }
        public Empleado? Empleado { get; set; } // El Vendedor que cerró el trato

        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }

    public class DetalleVenta
    {
        public int Id { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }

        public int IdVenta { get; set; }
        public Venta Venta { get; set; } = null!;

        public int IdProducto { get; set; }
        public Producto Producto { get; set; } = null!;
    }
}
