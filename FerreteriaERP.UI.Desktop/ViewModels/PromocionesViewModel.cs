using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using FerreteriaERP.Infrastructure.Data;
using FerreteriaERP.UI.Desktop.Views;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public record OpcionSucursalPromocion(int? IdSucursal, string Nombre)
{
    public override string ToString() => Nombre;
}

/// <summary>
/// ViewModel de Administración de Promociones y Campañas Comerciales (PASO 29).
/// </summary>
public partial class PromocionesViewModel : ViewModelBase
{
    private readonly IPromocionRepository _promocionRepository;
    private readonly IDbContextFactory<FerreteriaDbContext> _dbContextFactory;

    private int? _idPromocionEditando;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloFormulario))]
    [NotifyPropertyChangedFor(nameof(TextoBotonGuardar))]
    private bool _esModoEdicion;

    [ObservableProperty]
    private string _nombreForm = string.Empty;

    [ObservableProperty]
    private string _descripcionForm = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsTipoVolumen))]
    [NotifyPropertyChangedFor(nameof(EtiquetaValorDescuento))]
    private string _tipoPromocionSeleccionado = "Porcentaje";

    [ObservableProperty]
    private string _valorDescuentoTexto = "15";

    [ObservableProperty]
    private string _cantidadMinimaTexto = "3";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsAlcanceProducto))]
    [NotifyPropertyChangedFor(nameof(EsAlcanceCategoria))]
    private string _alcanceSeleccionado = "Producto";

    [ObservableProperty]
    private Producto? _productoSeleccionadoForm;

    [ObservableProperty]
    private Categoria? _categoriaSeleccionadaForm;

    [ObservableProperty]
    private OpcionSucursalPromocion? _sucursalSeleccionadaForm;

    [ObservableProperty]
    private DateTime? _fechaInicioForm = DateTime.Today;

    [ObservableProperty]
    private DateTime? _fechaFinForm = DateTime.Today.AddDays(15);

    [ObservableProperty]
    private bool _estaActivaForm = true;

    [ObservableProperty]
    private int _totalActivasCount;

    [ObservableProperty]
    private int _totalProgramadasCount;

    [ObservableProperty]
    private int _totalAplicacionesCount;

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

    public PromocionesViewModel(
        IPromocionRepository promocionRepository,
        IDbContextFactory<FerreteriaDbContext> dbContextFactory)
    {
        _promocionRepository = promocionRepository;
        _dbContextFactory = dbContextFactory;

        _ = InicializarAsync();
    }

    public ObservableCollection<Promocion> ListaPromociones { get; } = new();

    public ObservableCollection<Producto> ProductosDisponibles { get; } = new();

    public ObservableCollection<Categoria> CategoriasDisponibles { get; } = new();

    public ObservableCollection<OpcionSucursalPromocion> OpcionesSucursal { get; } = new();

    public IReadOnlyList<string> TiposPromocion { get; } = new[]
    {
        "Porcentaje",
        "PrecioFijo",
        "Volumen"
    };

    public IReadOnlyList<string> AlcancesPromocion { get; } = new[]
    {
        "Producto",
        "Categoria",
        "Tienda"
    };

    public bool ControlesHabilitados => !EstaOcupado;

    public bool SinPromociones => !EstaOcupado && ListaPromociones.Count == 0;

    public bool EsTipoVolumen =>
        string.Equals(TipoPromocionSeleccionado, "Volumen", StringComparison.OrdinalIgnoreCase);

    public string EtiquetaValorDescuento =>
        string.Equals(TipoPromocionSeleccionado, "PrecioFijo", StringComparison.OrdinalIgnoreCase)
            ? "NUEVO PRECIO UNITARIO FIJO (BS) *"
            : "PORCENTAJE DE DESCUENTO (%) *";

    public bool EsAlcanceProducto =>
        string.Equals(AlcanceSeleccionado, "Producto", StringComparison.OrdinalIgnoreCase);

    public bool EsAlcanceCategoria =>
        string.Equals(AlcanceSeleccionado, "Categoria", StringComparison.OrdinalIgnoreCase);

    public string TituloFormulario => EsModoEdicion
        ? "Editar Campaña Comercial"
        : "Crear Nueva Promoción";

    public string TextoBotonGuardar => EstaOcupado
        ? "Guardando..."
        : (EsModoEdicion ? "Guardar Cambios" : "Crear Campaña");

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsMensajeExito;

    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsMensajeExito;

    [RelayCommand]
    public async Task InicializarAsync()
    {
        EstaOcupado = true;
        LimpiarMensaje();

        try
        {
            await CargarCatalogosSoporteAsync();
            await CargarPromocionesCoreAsync();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo cargar el módulo de promociones: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task CargarCatalogosSoporteAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var productos = await db.Productos
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var categorias = await db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        var sucursales = await db.Sucursales
            .AsNoTracking()
            .OrderByDescending(s => s.EsCentral)
            .ThenBy(s => s.Nombre)
            .ToListAsync();

        ProductosDisponibles.Clear();
        foreach (var p in productos)
            ProductosDisponibles.Add(p);

        CategoriasDisponibles.Clear();
        foreach (var c in categorias)
            CategoriasDisponibles.Add(c);

        OpcionesSucursal.Clear();
        OpcionesSucursal.Add(new OpcionSucursalPromocion(null, "Todas las sucursales (Global)"));
        foreach (var s in sucursales)
            OpcionesSucursal.Add(new OpcionSucursalPromocion(s.Id, s.Nombre));

        ProductoSeleccionadoForm ??= ProductosDisponibles.FirstOrDefault();
        CategoriaSeleccionadaForm ??= CategoriasDisponibles.FirstOrDefault();
        SucursalSeleccionadaForm ??= OpcionesSucursal.FirstOrDefault();
    }

    private async Task CargarPromocionesCoreAsync()
    {
        var promociones = await _promocionRepository.ObtenerTodasConRelacionesAsync();

        ListaPromociones.Clear();
        foreach (var promo in promociones)
        {
            // Enriquecer referencias si vinieron del fallback en memoria
            if (promo.IdProducto.HasValue && promo.Producto is null)
                promo.Producto = ProductosDisponibles.FirstOrDefault(p => p.Id == promo.IdProducto.Value);

            if (promo.IdCategoria.HasValue && promo.Categoria is null)
                promo.Categoria = CategoriasDisponibles.FirstOrDefault(c => c.Id == promo.IdCategoria.Value);

            ListaPromociones.Add(promo);
        }

        TotalActivasCount = ListaPromociones.Count(p => p.EsVigenteHoy);
        TotalProgramadasCount = ListaPromociones.Count(p => p.EstaActiva && p.FechaInicio > DateTime.UtcNow);
        TotalAplicacionesCount = ListaPromociones.Sum(p => p.CantidadUsos);

        OnPropertyChanged(nameof(SinPromociones));
    }

    [RelayCommand]
    private void LimpiarFormulario()
    {
        LimpiarMensaje();
        _idPromocionEditando = null;
        EsModoEdicion = false;

        NombreForm = string.Empty;
        DescripcionForm = string.Empty;
        TipoPromocionSeleccionado = "Porcentaje";
        ValorDescuentoTexto = "15";
        CantidadMinimaTexto = "3";
        AlcanceSeleccionado = "Producto";
        ProductoSeleccionadoForm = ProductosDisponibles.FirstOrDefault();
        CategoriaSeleccionadaForm = CategoriasDisponibles.FirstOrDefault();
        SucursalSeleccionadaForm = OpcionesSucursal.FirstOrDefault();
        FechaInicioForm = DateTime.Today;
        FechaFinForm = DateTime.Today.AddDays(15);
        EstaActivaForm = true;
    }

    [RelayCommand]
    private void EditarPromocion(Promocion? promo)
    {
        if (promo is null)
            return;

        LimpiarMensaje();
        _idPromocionEditando = promo.Id;
        EsModoEdicion = true;

        NombreForm = promo.Nombre;
        DescripcionForm = promo.Descripcion;
        TipoPromocionSeleccionado = TiposPromocion.Contains(promo.TipoPromocion)
            ? promo.TipoPromocion
            : "Porcentaje";
        ValorDescuentoTexto = promo.ValorDescuento.ToString("0.##", CultureInfo.InvariantCulture);
        CantidadMinimaTexto = Math.Max(1, promo.CantidadMinima).ToString();
        AlcanceSeleccionado = AlcancesPromocion.Contains(promo.Alcance)
            ? promo.Alcance
            : "Producto";

        if (promo.IdProducto.HasValue)
            ProductoSeleccionadoForm = ProductosDisponibles.FirstOrDefault(p => p.Id == promo.IdProducto.Value);

        if (promo.IdCategoria.HasValue)
            CategoriaSeleccionadaForm = CategoriasDisponibles.FirstOrDefault(c => c.Id == promo.IdCategoria.Value);

        SucursalSeleccionadaForm = OpcionesSucursal.FirstOrDefault(s => s.IdSucursal == promo.IdSucursal)
                                   ?? OpcionesSucursal.FirstOrDefault();

        FechaInicioForm = (promo.FechaInicio.Kind == DateTimeKind.Utc ? promo.FechaInicio.ToLocalTime() : promo.FechaInicio).Date;
        FechaFinForm = (promo.FechaFin.Kind == DateTimeKind.Utc ? promo.FechaFin.ToLocalTime() : promo.FechaFin).Date;
        EstaActivaForm = promo.EstaActiva;
    }

    [RelayCommand]
    private async Task GuardarPromocionAsync()
    {
        LimpiarMensaje();

        var nombre = NombreForm?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MostrarErrorEstado("Ingrese el nombre comercial de la promoción.");
            return;
        }

        var valorNorm = (ValorDescuentoTexto ?? "0").Trim().Replace(',', '.');
        if (!decimal.TryParse(valorNorm, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorDescuento) || valorDescuento <= 0m)
        {
            MostrarErrorEstado("Ingrese un valor de descuento mayor a 0.");
            return;
        }

        if (!string.Equals(TipoPromocionSeleccionado, "PrecioFijo", StringComparison.OrdinalIgnoreCase) &&
            valorDescuento > 95m)
        {
            MostrarErrorEstado("El porcentaje de descuento debe estar entre 1% y 95%.");
            return;
        }

        var cantMinima = 1;
        if (EsTipoVolumen)
        {
            if (!int.TryParse(CantidadMinimaTexto?.Trim(), out cantMinima) || cantMinima < 2)
            {
                MostrarErrorEstado("Para una promoción por Volumen, la cantidad mínima debe ser de al menos 2 unidades.");
                return;
            }
        }

        if (EsAlcanceProducto && ProductoSeleccionadoForm is null)
        {
            MostrarErrorEstado("Seleccione el producto al que aplicará la promoción.");
            return;
        }

        if (EsAlcanceCategoria && CategoriaSeleccionadaForm is null)
        {
            MostrarErrorEstado("Seleccione la categoría a la que aplicará la promoción.");
            return;
        }

        var inicioLocal = (FechaInicioForm ?? DateTime.Today).Date;
        var finLocal = (FechaFinForm ?? DateTime.Today.AddDays(15)).Date;
        if (finLocal < inicioLocal)
        {
            MostrarErrorEstado("La fecha de finalización no puede ser anterior a la fecha de inicio.");
            return;
        }

        var inicioUtc = DateTime.SpecifyKind(inicioLocal, DateTimeKind.Local).ToUniversalTime();
        // Fin del día seleccionado en hora local convertido a UTC
        var finUtc = DateTime.SpecifyKind(finLocal.AddDays(1).AddSeconds(-1), DateTimeKind.Local).ToUniversalTime();

        EstaOcupado = true;
        try
        {
            var promocionGuardar = new Promocion
            {
                Id = _idPromocionEditando ?? 0,
                Nombre = nombre,
                Descripcion = string.IsNullOrWhiteSpace(DescripcionForm) ? nombre : DescripcionForm.Trim(),
                TipoPromocion = TipoPromocionSeleccionado,
                ValorDescuento = valorDescuento,
                CantidadMinima = EsTipoVolumen ? cantMinima : 1,
                Alcance = AlcanceSeleccionado,
                IdProducto = EsAlcanceProducto ? ProductoSeleccionadoForm?.Id : null,
                Producto = EsAlcanceProducto ? ProductoSeleccionadoForm : null,
                IdCategoria = EsAlcanceCategoria ? CategoriaSeleccionadaForm?.Id : null,
                Categoria = EsAlcanceCategoria ? CategoriaSeleccionadaForm : null,
                IdSucursal = SucursalSeleccionadaForm?.IdSucursal,
                FechaInicio = inicioUtc,
                FechaFin = finUtc,
                EstaActiva = EstaActivaForm
            };

            await _promocionRepository.GuardarPromocionAsync(promocionGuardar);
            await CargarPromocionesCoreAsync();

            var eraEdicion = EsModoEdicion;
            LimpiarFormulario();

            var texto = eraEdicion
                ? $"Campaña '{nombre}' actualizada correctamente."
                : $"Campaña '{nombre}' creada y activada.";

            MostrarExitoEstado(texto);
            MostrarToast(texto, NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al guardar la promoción: {ex.Message}");
            MostrarToast("Error al guardar la promoción", NotificationType.Error);
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    [RelayCommand]
    private async Task ToggleActivaAsync(Promocion? promo)
    {
        if (promo is null)
            return;

        LimpiarMensaje();
        try
        {
            var nuevoEstado = !promo.EstaActiva;
            await _promocionRepository.CambiarEstadoActivaAsync(promo.Id, nuevoEstado);
            await CargarPromocionesCoreAsync();

            var texto = nuevoEstado
                ? $"Campaña '{promo.Nombre}' activada."
                : $"Campaña '{promo.Nombre}' pausada.";

            MostrarExitoEstado(texto);
            MostrarToast(texto, NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo cambiar el estado de la promoción: {ex.Message}");
        }
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
                "Promociones y Campañas",
                mensaje,
                tipo,
                TimeSpan.FromSeconds(4)));
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
