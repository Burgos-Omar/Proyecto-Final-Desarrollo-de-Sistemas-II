using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.UI.Desktop.Models;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class InventarioViewModel : ViewModelBase
{
    private static readonly string CarpetaImagenesLocales = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FerreteriaERP",
        "Imagenes");

    private readonly IProductoRepository _productoRepository;
    private readonly IRepository<Categoria> _categoriaRepository;
    private readonly IRepository<Proveedor> _proveedorRepository;
    private readonly ISesionService _sesionService;
    private readonly IImportacionExcelService _importacionService;
    private readonly IWhatsAppService _whatsAppService;

    private List<ProductoInventarioItem> _todosLosProductos = new();

    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    private ProductoInventarioItem? _productoSeleccionado;

    [ObservableProperty]
    private bool _formularioVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloFormulario))]
    [NotifyPropertyChangedFor(nameof(EtiquetaCampoStock))]
    [NotifyPropertyChangedFor(nameof(TextoBotonGuardar))]
    private bool _esModoEdicion;

    private int? _idProductoEditando;

    // Campos del formulario de Producto
    [ObservableProperty]
    private string _codigoBarrasForm = string.Empty;

    [ObservableProperty]
    private string _nombreForm = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneImagenSeleccionada))]
    [NotifyPropertyChangedFor(nameof(NombreArchivoImagenTexto))]
    private string? _rutaImagenSeleccionada;

    [ObservableProperty]
    private string _precioCompraTexto = string.Empty;

    [ObservableProperty]
    private string _precioVentaTexto = string.Empty;

    [ObservableProperty]
    private string _stockSucursalTexto = "0";

    [ObservableProperty]
    private string _stockMinimoTexto = "5";

    [ObservableProperty]
    private Categoria? _categoriaSeleccionada;

    [ObservableProperty]
    private Proveedor? _proveedorSeleccionado;

    // Creación rápida de Categoría / Proveedor desde el formulario
    [ObservableProperty]
    private bool _mostrarNuevaCategoria;

    [ObservableProperty]
    private string _nuevaCategoriaNombre = string.Empty;

    [ObservableProperty]
    private bool _mostrarNuevoProveedor;

    [ObservableProperty]
    private string _nuevoProveedorNombre = string.Empty;

    // Estado y alertas
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ControlesHabilitados))]
    [NotifyPropertyChangedFor(nameof(TextoBotonGuardar))]
    private bool _estaOcupado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private string? _mensajeEstado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private bool _esMensajeExito;

    [ObservableProperty]
    private int _totalProductosCount;

    [ObservableProperty]
    private int _productosStockBajoCount;

    public InventarioViewModel(
        IProductoRepository productoRepository,
        IRepository<Categoria> categoriaRepository,
        IRepository<Proveedor> proveedorRepository,
        ISesionService sesionService,
        IImportacionExcelService importacionService,
        IWhatsAppService whatsAppService)
    {
        _productoRepository = productoRepository;
        _categoriaRepository = categoriaRepository;
        _proveedorRepository = proveedorRepository;
        _sesionService = sesionService;
        _importacionService = importacionService;
        _whatsAppService = whatsAppService;

        _ = InicializarAsync();
    }

    public ObservableCollection<ProductoInventarioItem> Productos { get; } = new();

    public ObservableCollection<Categoria> Categorias { get; } = new();

    public ObservableCollection<Proveedor> Proveedores { get; } = new();

    public bool SinResultados => Productos.Count == 0;

    public bool ControlesHabilitados => !EstaOcupado;

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsMensajeExito;

    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsMensajeExito;

    public bool TieneImagenSeleccionada => !string.IsNullOrWhiteSpace(RutaImagenSeleccionada);

    public string NombreArchivoImagenTexto =>
        string.IsNullOrWhiteSpace(RutaImagenSeleccionada)
            ? "Sin foto asignada (se usará el placeholder)"
            : Path.GetFileName(RutaImagenSeleccionada);

    public string NombreSucursalActiva =>
        _sesionService.UsuarioActual?.Sucursal?.Nombre ?? "Sucursal Activa";

    public string TituloFormulario => EsModoEdicion
        ? "Editar Producto"
        : "Nuevo Producto";

    public string EtiquetaCampoStock => EsModoEdicion
        ? "STOCK ACTUAL (ESTA SUCURSAL)"
        : "STOCK INICIAL (ESTA SUCURSAL)";

    public string TextoBotonGuardar => EstaOcupado
        ? "Guardando..."
        : (EsModoEdicion ? "Guardar Cambios" : "Guardar Producto");

    partial void OnTextoBusquedaChanged(string value)
    {
        AplicarFiltroLocal();
    }

    private async Task InicializarAsync()
    {
        await CargarCatalogosAuxiliaresAsync();
        await CargarProductosAsync();
    }

    [RelayCommand]
    private async Task RecargarAsync()
    {
        LimpiarMensaje();
        await CargarCatalogosAuxiliaresAsync();
        await CargarProductosAsync();
    }

    /// <summary>
    /// Abre el selector nativo de archivos para elegir una imagen (.png, .jpg, .jpeg),
    /// la copia a <c>%APPDATA%\FerreteriaERP\Imagenes</c> con un nombre único y actualiza la vista previa.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarImagenAsync()
    {
        LimpiarMensaje();

        var topLevel = ObtenerTopLevelActivo();
        if (topLevel?.StorageProvider is null)
        {
            MostrarErrorEstado("No se pudo abrir el explorador de archivos de la ventana principal.");
            return;
        }

        var filtroImagenes = new FilePickerFileType("Imágenes de Producto (*.png, *.jpg, *.jpeg)")
        {
            Patterns = new[] { "*.png", "*.jpg", "*.jpeg" },
            MimeTypes = new[] { "image/png", "image/jpeg" }
        };

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar foto del producto",
            AllowMultiple = false,
            FileTypeFilter = new[] { filtroImagenes }
        });

        var archivoSeleccionado = archivos.FirstOrDefault();
        if (archivoSeleccionado is null)
            return;

        var rutaOrigen = archivoSeleccionado.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(rutaOrigen) || !File.Exists(rutaOrigen))
        {
            MostrarErrorEstado("No se pudo acceder al archivo de imagen seleccionado.");
            return;
        }

        try
        {
            Directory.CreateDirectory(CarpetaImagenesLocales);

            var extension = Path.GetExtension(rutaOrigen);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".png";

            var nombreUnico = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
            var rutaDestino = Path.Combine(CarpetaImagenesLocales, nombreUnico);

            await Task.Run(() => File.Copy(rutaOrigen, rutaDestino, overwrite: true));

            RutaImagenSeleccionada = rutaDestino;
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo copiar la imagen seleccionada: {ex.Message}");
        }
    }

    [RelayCommand]
    private void QuitarImagen()
    {
        RutaImagenSeleccionada = null;
    }

    /// <summary>
    /// Abre WhatsApp con un mensaje prellenado para solicitar el reabastecimiento del producto a su proveedor.
    /// </summary>
    [RelayCommand]
    private void SolicitarReabastecimiento(ProductoInventarioItem? item)
    {
        if (item is null)
            return;

        LimpiarMensaje();

        var producto = item.Producto;
        var proveedor = producto.Proveedor
                        ?? Proveedores.FirstOrDefault(p => p.Id == producto.IdProveedor);

        if (proveedor is null)
        {
            MostrarErrorEstado($"El producto '{producto.Nombre}' no tiene un proveedor asociado.");
            return;
        }

        var celular = !string.IsNullOrWhiteSpace(proveedor.TelefonosContacto)
            ? proveedor.TelefonosContacto
            : proveedor.ContactoWhatsapp;

        if (string.IsNullOrWhiteSpace(celular))
        {
            MostrarErrorEstado(
                $"El proveedor '{proveedor.NombreEmpresa}' no tiene un número de celular/WhatsApp registrado. Edite su ficha en el módulo Proveedores.");
            return;
        }

        try
        {
            _whatsAppService.EnviarSolicitudReabastecimiento(
                celular,
                proveedor.NombreEmpresa,
                producto.Nombre,
                producto.CodigoBarras);

            MostrarExitoEstado(
                $"Se abrió WhatsApp para solicitar el reabastecimiento de '{producto.Nombre}' a '{proveedor.NombreEmpresa}'.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo enviar la solicitud por WhatsApp: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportarDesdeCsvAsync()
    {
        LimpiarMensaje();

        if (!_sesionService.EstaAutenticado)
        {
            MostrarErrorEstado("No hay una sesión activa para asociar la importación.");
            return;
        }

        var topLevel = ObtenerTopLevelActivo();
        if (topLevel?.StorageProvider is null)
        {
            MostrarErrorEstado("No se pudo abrir el selector de archivos de la ventana principal.");
            return;
        }

        var filtroCsv = new FilePickerFileType("Archivos CSV (*.csv)")
        {
            Patterns = new[] { "*.csv" },
            MimeTypes = new[] { "text/csv" }
        };

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar archivo CSV para importar al inventario",
            AllowMultiple = false,
            FileTypeFilter = new[] { filtroCsv }
        });

        var archivoSeleccionado = archivos.FirstOrDefault();
        if (archivoSeleccionado is null)
            return;

        var rutaLocal = archivoSeleccionado.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(rutaLocal))
        {
            MostrarErrorEstado("No se pudo obtener la ruta local del archivo CSV seleccionado.");
            return;
        }

        EstaOcupado = true;
        try
        {
            if (EsCsvDeProveedores(rutaLocal))
            {
                var proveedoresImportados = await _importacionService.ImportarProveedoresAsync(rutaLocal);
                await CargarCatalogosAuxiliaresAsync();

                MostrarExitoEstado(
                    $"Importación completada: {proveedoresImportados} proveedor(es) nuevo(s) registrado(s) desde '{Path.GetFileName(rutaLocal)}'.");
            }
            else
            {
                var idSucursalActual = _sesionService.IdSucursalActual;
                var productosImportados = await _importacionService.ImportarProductosAsync(rutaLocal, idSucursalActual);

                await CargarCatalogosAuxiliaresAsync();
                await CargarProductosAsync();

                MostrarExitoEstado(
                    $"Importación exitosa: {productosImportados} producto(s) procesado(s) en {NombreSucursalActiva} desde '{Path.GetFileName(rutaLocal)}'.");
            }
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al importar el archivo CSV: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private static TopLevel? ObtenerTopLevelActivo()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is not null)
        {
            return TopLevel.GetTopLevel(desktop.MainWindow);
        }

        return null;
    }

    private static bool EsCsvDeProveedores(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            var primeraLinea = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(primeraLinea))
                return false;

            return primeraLinea.Contains("NombreEmpresa", StringComparison.OrdinalIgnoreCase) &&
                   !primeraLinea.Contains("PrecioVenta", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task CargarCatalogosAuxiliaresAsync()
    {
        try
        {
            var idCatPrev = CategoriaSeleccionada?.Id;
            var idProvPrev = ProveedorSeleccionado?.Id;

            var listaCategorias = await _categoriaRepository.ObtenerTodosAsync();
            var listaProveedores = await _proveedorRepository.ObtenerTodosAsync();

            Categorias.Clear();
            foreach (var c in listaCategorias.OrderBy(x => x.Nombre))
                Categorias.Add(c);

            Proveedores.Clear();
            foreach (var p in listaProveedores.OrderBy(x => x.NombreEmpresa))
                Proveedores.Add(p);

            CategoriaSeleccionada = Categorias.FirstOrDefault(c => c.Id == idCatPrev)
                                    ?? Categorias.FirstOrDefault();

            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.Id == idProvPrev)
                                    ?? Proveedores.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al cargar categorías/proveedores: {ex.Message}");
        }
    }

    private async Task CargarProductosAsync()
    {
        if (!_sesionService.EstaAutenticado)
            return;

        EstaOcupado = true;
        try
        {
            var idSucursal = _sesionService.IdSucursalActual;
            var entidades = await _productoRepository.ObtenerCatalogoConStockSucursalAsync(idSucursal);

            _todosLosProductos = entidades.Select(p =>
            {
                var stockSucursal = p.Stocks.FirstOrDefault(s => s.IdSucursal == idSucursal)?.CantidadActual ?? 0;
                return new ProductoInventarioItem
                {
                    Producto = p,
                    StockActual = stockSucursal
                };
            }).ToList();

            TotalProductosCount = _todosLosProductos.Count;
            ProductosStockBajoCount = _todosLosProductos.Count(x => x.EsStockBajo);

            AplicarFiltroLocal();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo cargar el inventario: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AplicarFiltroLocal()
    {
        var filtro = TextoBusqueda?.Trim();

        IEnumerable<ProductoInventarioItem> filtrados = _todosLosProductos;
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            filtrados = _todosLosProductos.Where(item =>
                item.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                item.CodigoBarras.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                item.CategoriaNombre.Contains(filtro, StringComparison.OrdinalIgnoreCase));
        }

        Productos.Clear();
        foreach (var item in filtrados)
        {
            Productos.Add(item);
        }

        OnPropertyChanged(nameof(SinResultados));
    }

    [RelayCommand]
    private void NuevoProducto()
    {
        LimpiarMensaje();
        EsModoEdicion = false;
        _idProductoEditando = null;

        CodigoBarrasForm = string.Empty;
        NombreForm = string.Empty;
        RutaImagenSeleccionada = null;
        PrecioCompraTexto = string.Empty;
        PrecioVentaTexto = string.Empty;
        StockSucursalTexto = "0";
        StockMinimoTexto = "5";
        CategoriaSeleccionada = Categorias.FirstOrDefault();
        ProveedorSeleccionado = Proveedores.FirstOrDefault();

        FormularioVisible = true;
    }

    [RelayCommand]
    private void EditarProducto(ProductoInventarioItem? item)
    {
        var objetivo = item ?? ProductoSeleccionado;
        if (objetivo is null)
            return;

        LimpiarMensaje();
        ProductoSeleccionado = objetivo;
        EsModoEdicion = true;
        _idProductoEditando = objetivo.Id;

        CodigoBarrasForm = objetivo.Producto.CodigoBarras ?? string.Empty;
        NombreForm = objetivo.Nombre;
        RutaImagenSeleccionada = objetivo.Producto.ImagenUrl;
        PrecioCompraTexto = objetivo.PrecioCompra.ToString("0.00", CultureInfo.InvariantCulture);
        PrecioVentaTexto = objetivo.PrecioVenta.ToString("0.00", CultureInfo.InvariantCulture);
        StockSucursalTexto = objetivo.StockActual.ToString(CultureInfo.InvariantCulture);
        StockMinimoTexto = objetivo.StockMinimo.ToString(CultureInfo.InvariantCulture);

        CategoriaSeleccionada = Categorias.FirstOrDefault(c => c.Id == objetivo.Producto.IdCategoria)
                                ?? Categorias.FirstOrDefault();
        ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.Id == objetivo.Producto.IdProveedor)
                                ?? Proveedores.FirstOrDefault();

        FormularioVisible = true;
    }

    [RelayCommand]
    private void CerrarFormulario()
    {
        FormularioVisible = false;
        MostrarNuevaCategoria = false;
        MostrarNuevoProveedor = false;
    }

    [RelayCommand]
    private void AlternarNuevaCategoria()
    {
        MostrarNuevaCategoria = !MostrarNuevaCategoria;
        NuevaCategoriaNombre = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarNuevaCategoriaAsync()
    {
        var nombre = NuevaCategoriaNombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return;

        try
        {
            var creada = await _categoriaRepository.AgregarAsync(new Categoria { Nombre = nombre });
            Categorias.Add(creada);
            CategoriaSeleccionada = creada;
            NuevaCategoriaNombre = string.Empty;
            MostrarNuevaCategoria = false;
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo crear la categoría: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AlternarNuevoProveedor()
    {
        MostrarNuevoProveedor = !MostrarNuevoProveedor;
        NuevoProveedorNombre = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarNuevoProveedorAsync()
    {
        var nombre = NuevoProveedorNombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return;

        try
        {
            var creado = await _proveedorRepository.AgregarAsync(new Proveedor { NombreEmpresa = nombre });
            Proveedores.Add(creado);
            ProveedorSeleccionado = creado;
            NuevoProveedorNombre = string.Empty;
            MostrarNuevoProveedor = false;
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo crear el proveedor: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GuardarProductoAsync()
    {
        LimpiarMensaje();

        if (string.IsNullOrWhiteSpace(NombreForm))
        {
            MostrarErrorEstado("El nombre del producto es obligatorio.");
            return;
        }

        if (CategoriaSeleccionada is null)
        {
            MostrarErrorEstado("Seleccione o cree una Categoría para el producto.");
            return;
        }

        if (ProveedorSeleccionado is null)
        {
            MostrarErrorEstado("Seleccione o cree un Proveedor para el producto.");
            return;
        }

        if (!IntentarParsearDecimal(PrecioCompraTexto, out var precioCompra) || precioCompra < 0m)
        {
            MostrarErrorEstado("Ingrese un Precio de Compra válido (mayor o igual a 0).");
            return;
        }

        if (!IntentarParsearDecimal(PrecioVentaTexto, out var precioVenta) || precioVenta <= 0m)
        {
            MostrarErrorEstado("Ingrese un Precio de Venta válido (mayor a 0).");
            return;
        }

        if (!int.TryParse(StockSucursalTexto?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stockSucursal)
            || stockSucursal < 0)
        {
            MostrarErrorEstado("Ingrese una cantidad de Stock válida (entero mayor o igual a 0).");
            return;
        }

        if (!int.TryParse(StockMinimoTexto?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stockMinimo)
            || stockMinimo < 0)
        {
            MostrarErrorEstado("Ingrese un Stock Mínimo válido (entero mayor o igual a 0).");
            return;
        }

        EstaOcupado = true;
        try
        {
            var idSucursal = _sesionService.IdSucursalActual;
            var entidadProducto = new Producto
            {
                Id = _idProductoEditando ?? 0,
                CodigoBarras = string.IsNullOrWhiteSpace(CodigoBarrasForm) ? null : CodigoBarrasForm.Trim(),
                Nombre = NombreForm.Trim(),
                ImagenUrl = string.IsNullOrWhiteSpace(RutaImagenSeleccionada) ? null : RutaImagenSeleccionada.Trim(),
                PrecioCompra = precioCompra,
                PrecioVenta = precioVenta,
                StockMinimo = stockMinimo,
                IdCategoria = CategoriaSeleccionada.Id,
                IdProveedor = ProveedorSeleccionado.Id
            };

            if (EsModoEdicion && _idProductoEditando is not null)
            {
                await _productoRepository.ActualizarConStockSucursalAsync(
                    entidadProducto, idSucursal, stockSucursal);
                MostrarExitoEstado($"Producto '{entidadProducto.Nombre}' actualizado correctamente.");
            }
            else
            {
                await _productoRepository.CrearConStockInicialAsync(
                    entidadProducto, idSucursal, stockSucursal);
                MostrarExitoEstado(
                    $"Producto '{entidadProducto.Nombre}' registrado con {stockSucursal} unidades en {NombreSucursalActiva}.");
            }

            FormularioVisible = false;
            await CargarProductosAsync();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado(ex.Message);
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private static bool IntentarParsearDecimal(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        var limpio = texto.Trim().Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    private void MostrarErrorEstado(string mensaje)
    {
        EsMensajeExito = false;
        MensajeEstado = mensaje;
    }

    private void MostrarExitoEstado(string mensaje)
    {
        EsMensajeExito = true;
        MensajeEstado = mensaje;
    }

    private void LimpiarMensaje()
    {
        MensajeEstado = null;
    }
}
