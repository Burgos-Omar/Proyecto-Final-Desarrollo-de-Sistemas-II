using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.UI.Desktop.Models;

/// <summary>
/// Modelo de tarjeta/ítem de Inventario que combina los datos maestros del <see cref="Producto"/>
/// con el stock físico disponible en la sucursal activa.
/// </summary>
public class ProductoInventarioItem
{
    public required Producto Producto { get; init; }

    public int Id => Producto.Id;

    public string CodigoBarras => string.IsNullOrWhiteSpace(Producto.CodigoBarras)
        ? "S/C"
        : Producto.CodigoBarras;

    public string Nombre => Producto.Nombre;

    public string? ImagenUrl => Producto.ImagenUrl;

    public string CategoriaNombre => Producto.Categoria?.Nombre ?? "Sin categoría";

    public string ProveedorNombre => Producto.Proveedor?.NombreEmpresa ?? "Sin proveedor";

    public decimal PrecioCompra => Producto.PrecioCompra;

    public decimal PrecioVenta => Producto.PrecioVenta;

    public int StockActual { get; init; }

    public int StockMinimo => Producto.StockMinimo;

    /// <summary>True cuando el stock físico de esta sucursal es menor o igual al stock mínimo.</summary>
    public bool EsStockBajo => StockActual <= StockMinimo;

    public bool EsAgotado => StockActual <= 0;

    public bool EsStockBajoNoAgotado => StockActual > 0 && StockActual <= StockMinimo;

    public bool EnStockNormal => !EsStockBajo;

    public string EstadoStockTexto => EsAgotado ? "AGOTADO" : (EsStockBajo ? "BAJO" : "OK");

    public string EstadoStockBadgeTexto => EsAgotado
        ? "Agotado (0)"
        : (EsStockBajo
            ? $"Stock Bajo ({StockActual} / mín. {StockMinimo})"
            : $"En Stock ({StockActual})");
}
