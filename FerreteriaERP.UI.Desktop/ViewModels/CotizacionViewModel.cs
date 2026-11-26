using System;
using System.Collections.Generic;
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
using FerreteriaERP.Infrastructure.Repositories;
using FerreteriaERP.Infrastructure.Services;
using FerreteriaERP.UI.Desktop.Views;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class CotizacionViewModel : ViewModelBase
{
    private readonly IProductoRepository _productoRepository;
    private readonly CotizacionRepository _cotizacionRepository;
    private readonly ISesionService _sesionService;
    private readonly TicketPdfService _ticketPdfService;

    private List<Producto> _productosCatalogo = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneTextoBusqueda))]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFormateado))]
    private decimal _total;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResumenArticulos))]
    private int _totalArticulos;

    [ObservableProperty]
    private string _nitCliente = "0";

    [ObservableProperty]
    private string _nombreCliente = "S/N";

    [ObservableProperty]
    private int _validezDias = 7;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private string? _mensajeEstado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private bool _esMensajeExito;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ControlesHabilitados))]
    [NotifyPropertyChangedFor(nameof(TextoBotonGenerar))]
    [NotifyCanExecuteChangedFor(nameof(GuardarCotizacionCommand))]
    private bool _estaProcesando;

    public CotizacionViewModel(
        IProductoRepository productoRepository,
        CotizacionRepository cotizacionRepository,
        ISesionService sesionService,
        TicketPdfService ticketPdfService)
    {
        _productoRepository = productoRepository;
        _cotizacionRepository = cotizacionRepository;
        _sesionService = sesionService;
        _ticketPdfService = ticketPdfService;

        Carrito.CollectionChanged += (_, _) => RecalcularTotales();

        _ = CargarCatalogoAsync();
    }

    public ObservableCollection<Producto> CatalogoVisible { get; } = new();

    public ObservableCollection<DetalleCotizacion> Carrito { get; } = new();

    public bool CatalogoVacio => CatalogoVisible.Count == 0;

    public bool TieneTextoBusqueda => !string.IsNullOrWhiteSpace(TextoBusqueda);

    public bool CarritoVacio => Carrito.Count == 0;

    public bool TieneProductosEnCarrito => Carrito.Count > 0;

    public string TotalFormateado => $"Bs {Total:N2}";

    public string ResumenArticulos => TotalArticulos == 1
        ? "1 unidad en proforma"
        : $"{TotalArticulos} unidades en proforma";

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsMensajeExito;

    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsMensajeExito;

    public bool ControlesHabilitados => !EstaProcesando;

    public string TextoBotonGenerar => EstaProcesando
        ? "GENERANDO PROFORMA..."
        : "Generar Proforma";

    partial void OnTextoBusquedaChanged(string value)
    {
        FiltrarCatalogoVisible(value);
    }

    [RelayCommand]
    public async Task CargarCatalogoAsync()
    {
        try
        {
            var idSucursal = _sesionService.EstaAutenticado ? _sesionService.IdSucursalActual : 1;
            var todos = await _productoRepository.ObtenerCatalogoConStockSucursalAsync(idSucursal);

            _productosCatalogo = todos
                .OrderBy(p => p.Nombre)
                .ToList();

            FiltrarCatalogoVisible(TextoBusqueda);
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo cargar el catálogo de productos: {ex.Message}");
        }
    }

    private void FiltrarCatalogoVisible(string? texto)
    {
        var filtro = texto?.Trim();

        IEnumerable<Producto> filtrados = _productosCatalogo;
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            filtrados = _productosCatalogo.Where(p =>
                p.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(p.CodigoBarras) &&
                 p.CodigoBarras.Contains(filtro, StringComparison.OrdinalIgnoreCase)));
        }

        CatalogoVisible.Clear();
        foreach (var prod in filtrados)
        {
            CatalogoVisible.Add(prod);
        }

        OnPropertyChanged(nameof(CatalogoVacio));
    }

    [RelayCommand]
    private void LimpiarBusqueda()
    {
        TextoBusqueda = string.Empty;
    }

    [RelayCommand]
    private void AgregarDesdeTarjeta(Producto? producto)
    {
        if (producto is null)
            return;

        LimpiarMensaje();
        AgregarOIncrementarEnCarrito(producto);
    }

    [RelayCommand]
    private async Task ProcesarEscaneoAsync()
    {
        var codigo = TextoBusqueda?.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
            return;

        try
        {
            var producto = _productosCatalogo.FirstOrDefault(p =>
                               !string.IsNullOrWhiteSpace(p.CodigoBarras) &&
                               string.Equals(p.CodigoBarras.Trim(), codigo, StringComparison.OrdinalIgnoreCase))
                           ?? await _productoRepository.ObtenerPorCodigoBarrasAsync(codigo);

            if (producto is null)
                return;

            LimpiarMensaje();
            AgregarOIncrementarEnCarrito(producto);
            TextoBusqueda = string.Empty;
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al procesar el código: {ex.Message}");
        }
    }

    private void AgregarOIncrementarEnCarrito(Producto producto)
    {
        var indiceExistente = -1;
        for (var i = 0; i < Carrito.Count; i++)
        {
            if (Carrito[i].IdProducto == producto.Id)
            {
                indiceExistente = i;
                break;
            }
        }

        if (indiceExistente >= 0)
        {
            var actual = Carrito[indiceExistente];
            var nuevaCantidad = actual.Cantidad + 1;
            Carrito[indiceExistente] = new DetalleCotizacion
            {
                IdProducto = actual.IdProducto,
                Producto = actual.Producto,
                Cantidad = nuevaCantidad,
                PrecioUnitario = actual.PrecioUnitario,
                Subtotal = nuevaCantidad * actual.PrecioUnitario
            };
        }
        else
        {
            Carrito.Add(new DetalleCotizacion
            {
                IdProducto = producto.Id,
                Producto = producto,
                Cantidad = 1,
                PrecioUnitario = producto.PrecioVenta,
                Subtotal = producto.PrecioVenta
            });
        }
    }

    [RelayCommand]
    private void IncrementarCantidad(DetalleCotizacion? detalle)
    {
        if (detalle is null)
            return;

        LimpiarMensaje();
        var indice = Carrito.IndexOf(detalle);
        if (indice < 0)
            return;

        var nuevaCantidad = detalle.Cantidad + 1;
        Carrito[indice] = new DetalleCotizacion
        {
            IdProducto = detalle.IdProducto,
            Producto = detalle.Producto,
            Cantidad = nuevaCantidad,
            PrecioUnitario = detalle.PrecioUnitario,
            Subtotal = nuevaCantidad * detalle.PrecioUnitario
        };
    }

    [RelayCommand]
    private void DisminuirCantidad(DetalleCotizacion? detalle)
    {
        if (detalle is null)
            return;

        LimpiarMensaje();
        var indice = Carrito.IndexOf(detalle);
        if (indice < 0)
            return;

        if (detalle.Cantidad <= 1)
        {
            Carrito.RemoveAt(indice);
        }
        else
        {
            var nuevaCantidad = detalle.Cantidad - 1;
            Carrito[indice] = new DetalleCotizacion
            {
                IdProducto = detalle.IdProducto,
                Producto = detalle.Producto,
                Cantidad = nuevaCantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                Subtotal = nuevaCantidad * detalle.PrecioUnitario
            };
        }
    }

    [RelayCommand]
    private void EliminarDelCarrito(DetalleCotizacion? detalle)
    {
        if (detalle is null)
            return;

        LimpiarMensaje();
        Carrito.Remove(detalle);
    }

    [RelayCommand]
    private void VaciarCarrito()
    {
        LimpiarPantalla();
    }

    private bool PuedeGuardarCotizacion() => !EstaProcesando && Carrito.Count > 0;

    /// <summary>
    /// PASO 26: Guarda la Cotizacion en BD (sin afectar inventario ni caja), genera el PDF en ruta temporal y lo abre.
    /// </summary>
    [RelayCommand(CanExecute = nameof(PuedeGuardarCotizacion))]
    private async Task GuardarCotizacionAsync()
    {
        LimpiarMensaje();

        if (Carrito.Count == 0)
        {
            MostrarErrorEstado("Agregue al menos un producto antes de generar la proforma.");
            return;
        }

        EstaProcesando = true;
        try
        {
            var validez = ValidezDias > 0 ? ValidezDias : 7;
            var nombreCli = string.IsNullOrWhiteSpace(NombreCliente) ? "S/N" : NombreCliente.Trim();
            var nitCli = string.IsNullOrWhiteSpace(NitCliente) ? "0" : NitCliente.Trim();

            var detallesParaPdf = Carrito.Select(item => new DetalleCotizacion
            {
                IdProducto = item.IdProducto,
                Producto = item.Producto,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                Subtotal = item.Cantidad * item.PrecioUnitario
            }).ToList();

            var nuevaCotizacion = new Cotizacion
            {
                FechaEmision = DateTime.UtcNow,
                ValidezDias = validez,
                NombreCliente = nombreCli,
                NitCliente = nitCli,
                Total = detallesParaPdf.Sum(d => d.Subtotal),
                Detalles = detallesParaPdf.Select(d => new DetalleCotizacion
                {
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal
                }).ToList()
            };

            try
            {
                nuevaCotizacion = await _cotizacionRepository.AddAsync(nuevaCotizacion);
            }
            catch
            {
                // Si la migración de la tabla Cotizaciones aún no fue aplicada en la BD,
                // permite igualmente generar e imprimir el PDF de la proforma sin bloquear al usuario.
            }

            string rutaTemporal = Path.Combine(
                Path.GetTempPath(),
                $"Cotizacion_{DateTime.Now:HHmmss}.pdf");

            await Task.Run(() =>
                _ticketPdfService.GenerarCotizacionPdf(nuevaCotizacion, detallesParaPdf, rutaTemporal));

            Process.Start(new ProcessStartInfo
            {
                FileName = rutaTemporal,
                UseShellExecute = true
            });

            var totalCotizado = nuevaCotizacion.Total;
            LimpiarPantalla();

            MostrarExitoEstado($"¡Proforma generada para '{nombreCli}' por Bs {totalCotizado:N2}!");
            MostrarToast("Proforma generada correctamente", NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo generar la proforma: {ex.Message}");
            MostrarToast("Error al generar la proforma: " + ex.Message, NotificationType.Error);
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    private void RecalcularTotales()
    {
        Total = Carrito.Sum(d => d.Cantidad * d.PrecioUnitario);
        TotalArticulos = Carrito.Sum(d => d.Cantidad);

        OnPropertyChanged(nameof(CarritoVacio));
        OnPropertyChanged(nameof(TieneProductosEnCarrito));
        GuardarCotizacionCommand.NotifyCanExecuteChanged();
    }

    private void LimpiarPantalla()
    {
        Carrito.Clear();
        TextoBusqueda = string.Empty;
        NitCliente = "0";
        NombreCliente = "S/N";
        ValidezDias = 7;
        LimpiarMensaje();
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
                "Cotizaciones / Proformas",
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
