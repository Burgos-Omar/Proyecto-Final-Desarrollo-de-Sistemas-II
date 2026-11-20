using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace FerreteriaERP.Core.Entities
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }

    public class Proveedor
    {
        public int Id { get; set; }
        public string NombreEmpresa { get; set; } = null!;
        public string? ContactoWhatsapp { get; set; }

        // 1. Datos Legales y de Contacto
        public string? Nit { get; set; }
        public string? NombreComercial { get; set; }
        public string? LicenciaFuncionamiento { get; set; }
        public string? CiContacto { get; set; }
        public string? DireccionAlmacen { get; set; }
        public string? TelefonosContacto { get; set; }
        public string? EmailInstitucional { get; set; }

        // 2. Términos Comerciales y Logísticos
        public string? FormasPagoAceptadas { get; set; }
        public string? MonedaFacturacion { get; set; } = "BOB";
        public string? TiemposEntrega { get; set; }
        public string? PoliticaFletes { get; set; }
        public string? PoliticaGarantiaYCambios { get; set; }

        // 3. Documentación Adjunta (PDF / DOCX en %APPDATA%\FerreteriaERP\Documentos)
        public string? RutaDocumentoAdjunto { get; set; }
        
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();

        // Propiedades auxiliares de solo lectura para la UI (ignoradas por EF Core)
        [NotMapped]
        public string NombreComercialMostrar =>
            string.IsNullOrWhiteSpace(NombreComercial) ? NombreEmpresa : NombreComercial!;

        [NotMapped]
        public string NitMostrar =>
            string.IsNullOrWhiteSpace(Nit) ? "Sin NIT registrado" : $"NIT: {Nit}";

        [NotMapped]
        public string TelefonoWhatsappEfectivo =>
            !string.IsNullOrWhiteSpace(ContactoWhatsapp)
                ? ContactoWhatsapp!
                : (TelefonosContacto ?? string.Empty);

        [NotMapped]
        public string TelefonoMostrar =>
            string.IsNullOrWhiteSpace(TelefonoWhatsappEfectivo)
                ? "Sin teléfono"
                : TelefonoWhatsappEfectivo;

        [NotMapped]
        public string MonedaMostrar =>
            string.IsNullOrWhiteSpace(MonedaFacturacion) ? "BOB" : MonedaFacturacion!;

        [NotMapped]
        public bool TieneDocumentoAdjunto =>
            !string.IsNullOrWhiteSpace(RutaDocumentoAdjunto);

        [NotMapped]
        public string NombreDocumentoAdjunto =>
            string.IsNullOrWhiteSpace(RutaDocumentoAdjunto)
                ? "Sin documento adjunto"
                : Path.GetFileName(RutaDocumentoAdjunto);
    }

    public class Producto
    {
        public int Id { get; set; }
        public string? CodigoBarras { get; set; }
        public string Nombre { get; set; } = null!;
        public string? FotoPath { get; set; }
        public string? ImagenUrl { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public int StockMinimo { get; set; }

        public int IdCategoria { get; set; }
        public Categoria Categoria { get; set; } = null!;

        public int IdProveedor { get; set; }
        public Proveedor Proveedor { get; set; } = null!;

        public ICollection<StockSucursal> Stocks { get; set; } = new List<StockSucursal>();
    }

    public class StockSucursal
    {
        public int Id { get; set; }
        public int CantidadActual { get; set; }

        public int IdProducto { get; set; }
        public Producto Producto { get; set; } = null!;

        public int IdSucursal { get; set; }
        public Sucursal Sucursal { get; set; } = null!;
    }
}
