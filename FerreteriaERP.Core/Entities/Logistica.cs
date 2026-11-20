using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerreteriaERP.Core.Entities
{
    public static class EstadosCargaEntrante
    {
        public const string EnTransito = "En Tránsito";
        public const string EnTerminal = "En Terminal";
        public const string Recibido = "Recibido";
        public const string ConObservaciones = "Con Observaciones";
    }

    public static class EstadosOrdenDespacho
    {
        public const string Pendiente = "Pendiente";
        public const string EnRuta = "En Ruta";
        public const string EntregadoParcial = "Entregado Parcial";
        public const string EntregadoCompleto = "Entregado Completo";
    }

    /// <summary>
    /// Representa un envío de mercadería despachado por un proveedor vía flota o camión hacia la ferretería.
    /// </summary>
    public class CargaEntrante
    {
        public int Id { get; set; }

        public int IdProveedor { get; set; }
        public Proveedor Proveedor { get; set; } = null!;

        /// <summary>Sucursal que recibirá la mercadería.</summary>
        public int IdSucursal { get; set; }
        public Sucursal Sucursal { get; set; } = null!;

        /// <summary>Fecha UTC exacta en que se registró la orden (independiente de la llegada estimada).</summary>
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        /// <summary>Valor de la mercadería comprada (sin incluir el flete).</summary>
        public decimal MontoTotal { get; set; }

        /// <summary>Producto principal asociado al lote (opcional, para ingreso rápido de stock al recibir).</summary>
        public int? IdProducto { get; set; }
        public Producto? Producto { get; set; }

        /// <summary>Cantidad de unidades en la guía para sumar al inventario cuando se recibe la carga.</summary>
        public int CantidadUnidades { get; set; }

        public string NumeroGuia { get; set; } = null!;
        public string EmpresaTransporte { get; set; } = null!;
        public decimal CostoFleteDestino { get; set; }
        public DateTime FechaEstimadaLlegada { get; set; } = DateTime.UtcNow.AddDays(2);

        /// <summary>En Tránsito, En Terminal, Recibido, Con Observaciones.</summary>
        public string Estado { get; set; } = EstadosCargaEntrante.EnTransito;

        public string? Observaciones { get; set; }

        /// <summary>Indica si las unidades de esta guía ya fueron sumadas al stock de la sucursal.</summary>
        public bool StockActualizado { get; set; }

        // Propiedades calculadas para la UI (ignoradas por EF Core)
        [NotMapped]
        public string NombreProveedorMostrar =>
            Proveedor?.NombreEmpresa ?? $"Proveedor #{IdProveedor}";

        [NotMapped]
        public string DetalleProductoTexto =>
            Producto is not null
                ? $"{Producto.Nombre} ({CantidadUnidades} u.)"
                : (CantidadUnidades > 0 ? $"Carga general ({CantidadUnidades} bultos/u.)" : "Carga general de mercadería");

        [NotMapped]
        public string FechaEstimadaTexto =>
            FechaEstimadaLlegada.ToLocalTime().ToString("dd/MM/yyyy");

        [NotMapped]
        public bool EsEnTransito =>
            string.Equals(Estado, EstadosCargaEntrante.EnTransito, StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool EsEnTerminal =>
            string.Equals(Estado, EstadosCargaEntrante.EnTerminal, StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool EsRecibido =>
            string.Equals(Estado, EstadosCargaEntrante.Recibido, StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool EsConObservaciones =>
            string.Equals(Estado, EstadosCargaEntrante.ConObservaciones, StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool NoEstaRecibido => !EsRecibido;
    }

    /// <summary>
    /// Representa una orden de despacho de materiales vendidos hacia una obra o domicilio del cliente.
    /// </summary>
    public class OrdenDespacho
    {
        public int Id { get; set; }

        public int IdVenta { get; set; }
        public Venta Venta { get; set; } = null!;

        public string DireccionEntrega { get; set; } = null!;

        /// <summary>Ej. "Camión Propio - Chofer Mario" o "Externo - Flota / Camioneta Flete".</summary>
        public string VehiculoChofer { get; set; } = null!;

        public decimal CostoFleteFerreteria { get; set; }

        /// <summary>Pendiente, En Ruta, Entregado Parcial, Entregado Completo.</summary>
        public string Estado { get; set; } = EstadosOrdenDespacho.Pendiente;

        public string? NombreReceptorObra { get; set; }

        public DateTime? FechaEntrega { get; set; }

        /// <summary>Total de unidades comprometidas en la venta para control de entregas parciales a obra.</summary>
        public int UnidadesTotales { get; set; } = 1;

        /// <summary>Unidades efectivamente entregadas hasta el momento en la obra.</summary>
        public int UnidadesEntregadas { get; set; }

        // Propiedades calculadas para la UI (ignoradas por EF Core)
        [NotMapped]
        public int UnidadesPendientes => Math.Max(0, UnidadesTotales - UnidadesEntregadas);

        [NotMapped]
        public string ProgresoEntregaTexto =>
            $"{UnidadesEntregadas} / {UnidadesTotales} u. entregadas ({UnidadesPendientes} pendientes)";

        [NotMapped]
        public string VentaClienteTexto =>
            Venta?.Cliente is not null
                ? $"Venta #{IdVenta} • {Venta.Cliente.NombreCompleto}"
                : $"Venta #{IdVenta}";

        [NotMapped]
        public string ReceptorMostrar =>
            string.IsNullOrWhiteSpace(NombreReceptorObra)
                ? "Pendiente de recepción en obra"
                : $"Recibió en obra: {NombreReceptorObra}";

        [NotMapped]
        public string FechaEntregaTexto =>
            FechaEntrega.HasValue
                ? FechaEntrega.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : "En programación";

        [NotMapped]
        public bool EsPendiente =>
            string.Equals(Estado, EstadosOrdenDespacho.Pendiente, StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool EsEnRuta =>
            string.Equals(Estado, EstadosOrdenDespacho.EnRuta, StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool EsEntregadoParcial =>
            string.Equals(Estado, EstadosOrdenDespacho.EntregadoParcial, StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool EsEntregadoCompleto =>
            string.Equals(Estado, EstadosOrdenDespacho.EntregadoCompleto, StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public bool PuedeDespachar => !EsEntregadoCompleto;
    }
}

