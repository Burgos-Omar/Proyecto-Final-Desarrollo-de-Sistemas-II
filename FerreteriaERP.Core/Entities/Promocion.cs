using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerreteriaERP.Core.Entities;

/// <summary>
/// Representa una promoción o campaña comercial aplicable por Producto, Categoría o toda la Tienda,
/// con reglas por Porcentaje, PrecioFijo o Volumen (PASO 29).
/// </summary>
public class Promocion
{
    private string _nombre = string.Empty;
    private string _titulo = string.Empty;

    public int Id { get; set; }

    /// <summary>Nombre comercial de la campaña (ej. "Semana de la Pintura -15%").</summary>
    public string Nombre
    {
        get => !string.IsNullOrWhiteSpace(_nombre) ? _nombre : _titulo;
        set
        {
            _nombre = value ?? string.Empty;
            _titulo = _nombre;
        }
    }

    /// <summary>Alias sincronizado con <see cref="Nombre"/> para compatibilidad con el banner de Inicio.</summary>
    public string Titulo
    {
        get => !string.IsNullOrWhiteSpace(_titulo) ? _titulo : _nombre;
        set
        {
            _titulo = value ?? string.Empty;
            _nombre = _titulo;
        }
    }

    public string Descripcion { get; set; } = string.Empty;

    /// <summary>"Porcentaje", "PrecioFijo", "Volumen".</summary>
    public string TipoPromocion { get; set; } = "Porcentaje";

    /// <summary>Porcentaje de descuento (ej. 15) o nuevo precio unitario fijo en Bs.</summary>
    public decimal ValorDescuento { get; set; }

    /// <summary>Cantidad mínima de unidades para activar la promoción (usado en "Volumen").</summary>
    public int CantidadMinima { get; set; } = 1;

    /// <summary>"Producto", "Categoria", "Tienda".</summary>
    public string Alcance { get; set; } = "Producto";

    public int? IdProducto { get; set; }
    public Producto? Producto { get; set; }

    public int? IdCategoria { get; set; }
    public Categoria? Categoria { get; set; }

    /// <summary>Si es null, la promoción aplica a todas las sucursales.</summary>
    public int? IdSucursal { get; set; }
    public Sucursal? Sucursal { get; set; }

    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime FechaFin { get; set; } = DateTime.UtcNow.AddDays(30);
    public string? ImagenFondoUrl { get; set; }
    public bool EstaActiva { get; set; } = true;

    // Propiedades auxiliares de presentación para la UI (no mapeadas en BD)
    [NotMapped]
    public int CantidadUsos { get; set; }

    [NotMapped]
    public decimal MontoTotalGenerado { get; set; }

    [NotMapped]
    public string VigenciaTexto =>
        FechaFin < FechaInicio
            ? "Sin fecha de vigencia"
            : $"{FechaInicio.ToLocalTime():dd/MM/yyyy} al {FechaFin.ToLocalTime():dd/MM/yyyy}";

    [NotMapped]
    public string ReglaResumenTexto => TipoPromocion switch
    {
        "PrecioFijo" => $"Precio Fijo: Bs. {ValorDescuento:N2}",
        "Volumen" => $"-{ValorDescuento:0.##}% (desde {CantidadMinima} u.)",
        _ => $"-{ValorDescuento:0.##}% de descuento"
    };

    [NotMapped]
    public string AlcanceResumenTexto => Alcance switch
    {
        "Producto" => Producto is not null ? $"Producto: {Producto.Nombre}" : (IdProducto.HasValue ? $"Producto #{IdProducto}" : "Producto específico"),
        "Categoria" => Categoria is not null ? $"Categoría: {Categoria.Nombre}" : (IdCategoria.HasValue ? $"Categoría #{IdCategoria}" : "Categoría específica"),
        _ => "Toda la Tienda"
    };

    [NotMapped]
    public string SucursalResumenTexto =>
        Sucursal is not null
            ? Sucursal.Nombre
            : (IdSucursal.HasValue ? $"Sucursal #{IdSucursal}" : "Todas las sucursales");

    [NotMapped]
    public string EstadoVigenciaTexto
    {
        get
        {
            if (!EstaActiva)
                return "Pausada";

            var hoy = DateTime.UtcNow;
            if (hoy < FechaInicio)
                return "Programada";

            if (hoy > FechaFin)
                return "Vencida";

            return "Activa";
        }
    }

    [NotMapped]
    public bool EsVigenteHoy
    {
        get
        {
            if (!EstaActiva)
                return false;

            var ahora = DateTime.UtcNow;
            return ahora >= FechaInicio && ahora <= FechaFin;
        }
    }

    [NotMapped]
    public string TextoBotonAccionEstado => EstaActiva ? "Pausar" : "Activar";
}
