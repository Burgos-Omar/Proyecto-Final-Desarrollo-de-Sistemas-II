using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Services;
using FerreteriaERP.Infrastructure.Data;
using FerreteriaERP.Infrastructure.Services;
using FerreteriaERP.UI.Desktop.Views;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.UI.Desktop.ViewModels;

/// <summary>
/// Fila editable de un producto con stock crítico/bajo para armar una Orden de Compra.
/// </summary>
public partial class ProductoReabastecimientoItem : ObservableObject
{
    private readonly Action _onCambioResumen;

    [ObservableProperty]
    private bool _seleccionado = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubtotalEstimado))]
    [NotifyPropertyChangedFor(nameof(SubtotalEstimadoTexto))]
    private int _cantidadAPedir;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubtotalEstimado))]
    [NotifyPropertyChangedFor(nameof(SubtotalEstimadoTexto))]
    private decimal _costoUnitarioEstimado;

    public ProductoReabastecimientoItem(Producto producto, int stockActual, Action onCambioResumen)
    {
        Producto = producto;
        StockActual = Math.Max(0, stockActual);
        StockMinimo = Math.Max(0, producto.StockMinimo);
        _onCambioResumen = onCambioResumen;

        var sugerido = (StockMinimo * 2) - StockActual;
        _cantidadAPedir = Math.Max(5, sugerido);
        _costoUnitarioEstimado = producto.PrecioCompra > 0m ? producto.PrecioCompra : producto.PrecioVenta;
    }

    public Producto Producto { get; }
    public int StockActual { get; }
    public int StockMinimo { get; }

    public string Nombre => Producto.Nombre;
    public string CodigoBarras => string.IsNullOrWhiteSpace(Producto.CodigoBarras) ? $"#{Producto.Id}" : Producto.CodigoBarras!;

    public bool EsAgotado => StockActual <= 0;
    public bool EsBajo => StockActual > 0;
    public string EstadoBadgeTexto => EsAgotado ? "Agotado" : "Bajo";

    public decimal SubtotalEstimado => Math.Max(0, CantidadAPedir) * Math.Max(0m, CostoUnitarioEstimado);
    public string SubtotalEstimadoTexto => $"Bs. {SubtotalEstimado:N2}";

    partial void OnSeleccionadoChanged(bool value) => _onCambioResumen();
    partial void OnCantidadAPedirChanged(int value) => _onCambioResumen();
    partial void OnCostoUnitarioEstimadoChanged(decimal value) => _onCambioResumen();
}

/// <summary>
/// ViewModel del Módulo de Reabastecimiento y Órdenes de Compra (PASO 30).
/// </summary>
public partial class ReabastecimientoViewModel : ViewModelBase
{
    private readonly IDbContextFactory<FerreteriaDbContext> _dbContextFactory;
    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly OrdenCompraPdfService _ordenCompraPdfService;
    private readonly ISesionService _sesionService;

    [ObservableProperty]
    private Proveedor? _proveedorSeleccionado;

    [ObservableProperty]
    private string _observacionesOrden = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalEstimadoTexto))]
    private decimal _totalEstimadoOrden;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResumenSeleccionTexto))]
    private int _cantidadSeleccionados;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ControlesHabilitados))]
    [NotifyPropertyChangedFor(nameof(TextoBotonGenerar))]
    private bool _estaOcupado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private string? _mensajeEstado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private bool _esMensajeExito;

    public ReabastecimientoViewModel(
        IDbContextFactory<FerreteriaDbContext> dbContextFactory,
        IOrdenCompraRepository ordenCompraRepository,
        OrdenCompraPdfService ordenCompraPdfService,
        ISesionService sesionService)
    {
        _dbContextFactory = dbContextFactory;
        _ordenCompraRepository = ordenCompraRepository;
        _ordenCompraPdfService = ordenCompraPdfService;
        _sesionService = sesionService;

        _ = InicializarAsync();
    }

    public ObservableCollection<ProductoReabastecimientoItem> ProductosStockCritico { get; } = new();

    public ObservableCollection<Proveedor> ProveedoresDisponibles { get; } = new();

    public ObservableCollection<OrdenCompra> HistorialOrdenes { get; } = new();

    public bool ControlesHabilitados => !EstaOcupado;

    public bool SinProductosCriticos => !EstaOcupado && ProductosStockCritico.Count == 0;

    public bool SinHistorialOrdenes => !EstaOcupado && HistorialOrdenes.Count == 0;

    public string TotalEstimadoTexto => $"Bs. {TotalEstimadoOrden:N2}";

    public string ResumenSeleccionTexto => CantidadSeleccionados == 1
        ? "1 producto seleccionado"
        : $"{CantidadSeleccionados} productos seleccionados";

    public string TextoBotonGenerar => EstaOcupado
        ? "Generando Orden..."
        : "Generar Orden de Compra (PDF)";

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsMensajeExito;

    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsMensajeExito;

    [RelayCommand]
    public async Task InicializarAsync()
    {
        EstaOcupado = true;
        LimpiarMensaje();

        try
        {
            await CargarDatosCoreAsync();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo cargar el módulo de reabastecimiento: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
            OnPropertyChanged(nameof(SinProductosCriticos));
            OnPropertyChanged(nameof(SinHistorialOrdenes));
        }
    }

    private async Task CargarDatosCoreAsync()
    {
        var idSucursal = SesionActual.IdSucursal > 0
            ? SesionActual.IdSucursal
            : (_sesionService.EstaAutenticado ? _sesionService.IdSucursalActual : 1);

        await using var db = await _dbContextFactory.CreateDbContextAsync();

        // 1. Proveedores disponibles
        var proveedores = await db.Proveedores
            .AsNoTracking()
            .OrderBy(p => p.NombreEmpresa)
            .ToListAsync();

        var idProvPrevio = ProveedorSeleccionado?.Id;
        ProveedoresDisponibles.Clear();
        foreach (var prov in proveedores)
            ProveedoresDisponibles.Add(prov);

        ProveedorSeleccionado = ProveedoresDisponibles.FirstOrDefault(p => p.Id == idProvPrevio)
                                ?? ProveedoresDisponibles.FirstOrDefault();

        // 2. Productos con Stock Crítico / Bajo (CantidadActual <= StockMinimo) en la sucursal actual
        var productos = await db.Productos
            .AsNoTracking()
            .Include(p => p.Stocks.Where(s => s.IdSucursal == idSucursal))
            .ToListAsync();

        var criticos = productos
            .Select(p => new
            {
                Producto = p,
                StockActual = Math.Max(0, p.Stocks.FirstOrDefault(s => s.IdSucursal == idSucursal)?.CantidadActual ?? 0)
            })
            .Where(x => x.StockActual <= x.Producto.StockMinimo)
            .OrderBy(x => x.StockActual == 0 ? 0 : 1)
            .ThenBy(x => x.StockActual)
            .ThenBy(x => x.Producto.Nombre)
            .ToList();

        ProductosStockCritico.Clear();
        foreach (var item in criticos)
        {
            ProductosStockCritico.Add(new ProductoReabastecimientoItem(
                item.Producto,
                item.StockActual,
                RecalcularResumenOrden));
        }

        RecalcularResumenOrden();

        // 3. Historial de Órdenes de Compra de la sucursal
        var historial = await _ordenCompraRepository.ObtenerHistorialAsync(idSucursal);
        HistorialOrdenes.Clear();
        foreach (var oc in historial)
        {
            HistorialOrdenes.Add(oc);
        }
    }

    private void RecalcularResumenOrden()
    {
        var seleccionados = ProductosStockCritico.Where(p => p.Seleccionado && p.CantidadAPedir > 0).ToList();
        CantidadSeleccionados = seleccionados.Count;
        TotalEstimadoOrden = seleccionados.Sum(p => p.SubtotalEstimado);
    }

    [RelayCommand]
    private async Task GenerarOrdenCompraAsync()
    {
        LimpiarMensaje();

        if (ProveedorSeleccionado is null)
        {
            MostrarErrorEstado("Seleccione un proveedor antes de generar la Orden de Compra.");
            return;
        }

        var itemsElegidos = ProductosStockCritico
            .Where(p => p.Seleccionado && p.CantidadAPedir > 0)
            .ToList();

        if (itemsElegidos.Count == 0)
        {
            MostrarErrorEstado("Seleccione al menos un producto con cantidad a pedir mayor a 0.");
            return;
        }

        EstaOcupado = true;
        try
        {
            var idSucursal = SesionActual.IdSucursal > 0
                ? SesionActual.IdSucursal
                : (_sesionService.EstaAutenticado ? _sesionService.IdSucursalActual : 1);

            var idUsuario = SesionActual.IdUsuario > 0
                ? SesionActual.IdUsuario
                : (_sesionService.EstaAutenticado ? _sesionService.IdUsuarioActual : 1);

            var sucursalActual = _sesionService.UsuarioActual?.Sucursal
                                 ?? new Sucursal { Id = idSucursal, Nombre = SesionActual.NombreSucursal };

            var nuevaOrden = new OrdenCompra
            {
                FechaEmision = DateTime.UtcNow,
                IdProveedor = ProveedorSeleccionado.Id,
                Proveedor = ProveedorSeleccionado,
                IdSucursal = idSucursal,
                Sucursal = sucursalActual,
                IdUsuario = idUsuario,
                Estado = EstadosOrdenCompra.Pendiente,
                Observaciones = string.IsNullOrWhiteSpace(ObservacionesOrden) ? null : ObservacionesOrden.Trim(),
                Detalles = itemsElegidos.Select(item => new DetalleOrdenCompra
                {
                    IdProducto = item.Producto.Id,
                    Producto = item.Producto,
                    CantidadSolicitada = item.CantidadAPedir,
                    CostoUnitarioEstimado = item.CostoUnitarioEstimado,
                    Subtotal = item.SubtotalEstimado
                }).ToList()
            };

            var ordenGuardada = await _ordenCompraRepository.CrearOrdenCompraAsync(nuevaOrden);

            // Generar PDF en Temp y abrirlo automáticamente
            await GenerarYAbrirPdfOrdenAsync(ordenGuardada);

            ObservacionesOrden = string.Empty;
            await CargarDatosCoreAsync();

            var texto = $"Orden de Compra {ordenGuardada.NumeroOrden} generada por {ordenGuardada.TotalEstimadoTexto}.";
            MostrarExitoEstado(texto);
            MostrarToast(texto, NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo generar la orden de compra: {ex.Message}");
            MostrarToast("Error al generar orden de compra: " + ex.Message, NotificationType.Error);
        }
        finally
        {
            EstaOcupado = false;
            OnPropertyChanged(nameof(SinProductosCriticos));
            OnPropertyChanged(nameof(SinHistorialOrdenes));
        }
    }

    [RelayCommand]
    private async Task VerPdfOrdenAsync(OrdenCompra? orden)
    {
        if (orden is null)
            return;

        try
        {
            await GenerarYAbrirPdfOrdenAsync(orden);
        }
        catch (Exception ex)
        {
            MostrarToast("Error al abrir el PDF de la orden: " + ex.Message, NotificationType.Error);
        }
    }

    [RelayCommand]
    private async Task RecibirMercaderiaAsync(OrdenCompra? orden)
    {
        if (orden is null || !orden.EsPendiente)
            return;

        LimpiarMensaje();
        EstaOcupado = true;
        try
        {
            await _ordenCompraRepository.MarcarComoRecibidaAsync(orden.Id);
            await CargarDatosCoreAsync();

            var texto = $"Orden {orden.NumeroOrden} marcada como Recibida. Stock actualizado en inventario.";
            MostrarExitoEstado(texto);
            MostrarToast(texto, NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo recibir la mercadería: {ex.Message}");
            MostrarToast("Error al recibir mercadería: " + ex.Message, NotificationType.Error);
        }
        finally
        {
            EstaOcupado = false;
            OnPropertyChanged(nameof(SinProductosCriticos));
            OnPropertyChanged(nameof(SinHistorialOrdenes));
        }
    }

    private async Task GenerarYAbrirPdfOrdenAsync(OrdenCompra orden)
    {
        var nombreSeguro = (orden.NumeroOrden ?? $"OC_{orden.Id}").Replace('/', '-').Replace('\\', '-');
        var rutaTemporal = Path.Combine(
            Path.GetTempPath(),
            $"OrdenCompra_{nombreSeguro}_{DateTime.Now:HHmmss}.pdf");

        await Task.Run(() => _ordenCompraPdfService.GenerarOrdenCompraPdf(orden, rutaTemporal));

        Process.Start(new ProcessStartInfo
        {
            FileName = rutaTemporal,
            UseShellExecute = true
        });
    }

    private static TopLevel? ObtenerTopLevelActivo()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var ventanaActiva = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
            if (ventanaActiva is not null)
                return TopLevel.GetTopLevel(ventanaActiva);
        }

        return null;
    }

    private static void MostrarToast(string mensaje, NotificationType tipo)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var manager = MainView.NotificationManagerActual;
            if (manager is null)
            {
                var topLevel = ObtenerTopLevelActivo();
                if (topLevel is not null)
                {
                    manager = new WindowNotificationManager(topLevel)
                    {
                        Position = NotificationPosition.TopRight,
                        MaxItems = 3
                    };
                }
            }

            manager?.Show(new Notification(
                "Reabastecimiento y Compras",
                mensaje,
                tipo,
                TimeSpan.FromSeconds(5)));
        });
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
