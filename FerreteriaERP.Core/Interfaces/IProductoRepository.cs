using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;

namespace FerreteriaERP.Core.Interfaces
{
    public interface IProductoRepository : IRepository<Producto>
    {
        /// <summary>
        /// Busca un producto por su código de barras exacto (flujo del escáner en el POS).
        /// Incluye la categoría. Devuelve null si no existe.
        /// </summary>
        Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);

        /// <summary>Búsqueda por nombre (sin distinguir mayúsculas), limitada a <paramref name="maximo"/> resultados.</summary>
        Task<IReadOnlyList<Producto>> BuscarPorNombreAsync(string texto, int maximo = 50, CancellationToken ct = default);

        /// <summary>Stock actual de un producto en una sucursal (0 si no tiene registro).</summary>
        Task<int> ObtenerStockAsync(int idProducto, int idSucursal, CancellationToken ct = default);

        /// <summary>Productos de una sucursal cuyo stock actual es menor o igual al stock mínimo.</summary>
        Task<IReadOnlyList<StockSucursal>> ObtenerStockBajoAsync(int idSucursal, CancellationToken ct = default);

        /// <summary>
        /// Suma o resta unidades al stock de una sucursal de forma atómica (delta negativo = salida).
        /// Lanza <see cref="Exceptions.StockInsuficienteException"/> si el stock quedaría negativo.
        /// </summary>
        Task AjustarStockAsync(int idProducto, int idSucursal, int delta, CancellationToken ct = default);

        /// <summary>
        /// Obtiene todos los productos con su Categoría, Proveedor y el registro de Stock correspondiente a <paramref name="idSucursal"/>.
        /// </summary>
        Task<IReadOnlyList<Producto>> ObtenerCatalogoConStockSucursalAsync(
            int idSucursal, string? filtro = null, CancellationToken ct = default);

        /// <summary>
        /// Inserta un nuevo producto en <c>Productos</c> y crea su registro inicial en <c>StockSucursales</c>
        /// para la sucursal indicada dentro de una misma transacción atómica.
        /// </summary>
        Task<Producto> CrearConStockInicialAsync(
            Producto producto, int idSucursal, int stockInicial, CancellationToken ct = default);

        /// <summary>
        /// Actualiza los datos de un producto y sincroniza el stock físico de la sucursal indicada en una transacción atómica.
        /// </summary>
        Task ActualizarConStockSucursalAsync(
            Producto producto, int idSucursal, int stockSucursal, CancellationToken ct = default);
    }
}
