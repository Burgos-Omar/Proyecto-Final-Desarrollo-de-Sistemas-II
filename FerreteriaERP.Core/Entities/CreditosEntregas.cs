using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerreteriaERP.Core.Entities
{
    public class Credito
    {
        public int Id { get; set; }
        public decimal MontoTotalDeuda { get; set; }
        public decimal SaldoPendiente { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Pagado

        public int IdVenta { get; set; }
        public Venta Venta { get; set; } = null!;

        public int IdCliente { get; set; }
        public Cliente Cliente { get; set; } = null!;

        public ICollection<AbonoCredito> Abonos { get; set; } = new List<AbonoCredito>();

        // Propiedades calculadas de solo lectura para la UI (ignoradas por EF Core)
        [NotMapped]
        public bool EsPendiente =>
            string.Equals(Estado, "Pendiente", StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool EsPagado =>
            string.Equals(Estado, "Pagado", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Indica si el crédito sigue Pendiente y su Fecha de Vencimiento es igual o menor al día de hoy.
        /// </summary>
        [NotMapped]
        public bool EsVencido =>
            EsPendiente &&
            FechaVencimiento.HasValue &&
            FechaVencimiento.Value.ToLocalTime().Date <= DateTime.Today;

        [NotMapped]
        public string FechaVencimientoTexto =>
            FechaVencimiento.HasValue
                ? FechaVencimiento.Value.ToLocalTime().ToString("dd/MM/yyyy")
                : "Sin fecha";

        [NotMapped]
        public string CelularClienteTexto =>
            string.IsNullOrWhiteSpace(Cliente?.CelularWhatsapp)
                ? "Sin registrar"
                : Cliente.CelularWhatsapp!;

        [NotMapped]
        public string EstadoDetalleTexto =>
            EsVencido ? "VENCIDO" : Estado.ToUpperInvariant();
    }

    public class AbonoCredito
    {
        public int Id { get; set; }
        public decimal MontoPagado { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.UtcNow;

        public int IdCredito { get; set; }
        public Credito Credito { get; set; } = null!;

        [NotMapped]
        public string FechaPagoTexto => FechaPago.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    public class Entrega
    {
        public int Id { get; set; }
        public string DireccionEnvio { get; set; } = null!;
        public string EstadoEntrega { get; set; } = "En Preparacion"; // En Preparacion, En Ruta, Entregado

        public int IdVenta { get; set; }
        public Venta Venta { get; set; } = null!;

        public int IdChofer { get; set; }
        public Usuario Chofer { get; set; } = null!;
    }
}
