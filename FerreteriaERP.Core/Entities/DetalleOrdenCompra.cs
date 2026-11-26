namespace FerreteriaERP.Core.Entities;

/// <summary>
/// Ítem individual solicitado dentro de una <see cref="OrdenCompra"/>.
/// </summary>
public class DetalleOrdenCompra
{
    public int Id { get; set; }

    public int IdOrdenCompra { get; set; }
    public OrdenCompra OrdenCompra { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public int CantidadSolicitada { get; set; }
    public decimal CostoUnitarioEstimado { get; set; }
    public decimal Subtotal { get; set; }
}
