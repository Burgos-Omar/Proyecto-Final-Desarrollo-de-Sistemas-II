namespace FerreteriaERP.Core.Interfaces
{
    /// <summary>
    /// Servicio para la importación masiva de catálogos (Proveedores y Productos con Stock)
    /// desde archivos CSV / Excel.
    /// </summary>
    public interface IImportacionExcelService
    {
        /// <summary>
        /// Lee las columnas (NombreEmpresa, ContactoWhatsapp) desde un archivo CSV y guarda
        /// únicamente los proveedores que aún no existen en la base de datos.
        /// </summary>
        /// <returns>Cantidad de proveedores nuevos insertados.</returns>
        Task<int> ImportarProveedoresAsync(string filePath, CancellationToken ct = default);

        /// <summary>
        /// Lee las columnas (CodigoBarras, Nombre, Categoria, Proveedor, PrecioCompra, PrecioVenta,
        /// StockInicial, StockMinimo) desde un archivo CSV. Si la Categoría o el Proveedor no existen,
        /// los crea al vuelo dentro de una misma transacción atómica junto con cada Producto y su StockSucursal.
        /// </summary>
        /// <returns>Cantidad de productos procesados exitosamente.</returns>
        Task<int> ImportarProductosAsync(string filePath, int idSucursalActual, CancellationToken ct = default);
    }
}

