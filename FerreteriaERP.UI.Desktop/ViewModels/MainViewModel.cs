using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Services;
using FerreteriaERP.UI.Desktop.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IConfiguracionConexionService? _configuracionService;
    private readonly IConfiguracionRepository? _configuracionRepository;
    private readonly ISesionService? _sesionService;
    private readonly IProductoRepository? _productoRepository;
    private readonly IServiceProvider? _serviceProvider;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarSidebar))]
    [NotifyPropertyChangedFor(nameof(EsVistaInicioActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaCajaActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaCotizacionesActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaInventarioActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaProveedoresActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaLogisticaActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaEmpleadosActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaCreditosActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaPromocionesActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaReabastecimientoActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaGerenciaActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaConfiguracionActiva))]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarSidebar))]
    [NotifyPropertyChangedFor(nameof(TieneAccesoAdministrativo))]
    [NotifyPropertyChangedFor(nameof(EsAdmin))]
    [NotifyPropertyChangedFor(nameof(EsSuperAdmin))]
    [NotifyPropertyChangedFor(nameof(PuedeVerReabastecimiento))]
    [NotifyPropertyChangedFor(nameof(NombreUsuarioActual))]
    [NotifyPropertyChangedFor(nameof(DetalleRolSucursal))]
    [NotifyPropertyChangedFor(nameof(InicialesUsuarioActual))]
    private Usuario? _usuarioActual;

    [ObservableProperty]
    private string _textoBusquedaGlobal = string.Empty;

    [ObservableProperty]
    private bool _isBuscando;

    [ObservableProperty]
    private ProductoInventarioItem? _productoBusquedaSeleccionado;

    [ObservableProperty]
    private bool _esModoOscuro;

    [ObservableProperty]
    private string _nombreNegocio = "Ferretería ERP";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneLogoPersonalizado))]
    private Bitmap? _logoNegocioBitmap;

    public bool TieneLogoPersonalizado => LogoNegocioBitmap is not null;

    /// <summary>
    /// Constructor sin parámetros requerido por el previsualizador (designer) de Avalonia.
    /// </summary>
    public MainViewModel()
    {
    }

    [ActivatorUtilitiesConstructor]
    public MainViewModel(
        IConfiguracionConexionService configuracionService,
        IConfiguracionRepository configuracionRepository,
        ISesionService sesionService,
        IProductoRepository productoRepository,
        IServiceProvider serviceProvider)
    {
        _configuracionService = configuracionService;
        _configuracionRepository = configuracionRepository;
        _sesionService = sesionService;
        _productoRepository = productoRepository;
        _serviceProvider = serviceProvider;

        _ = CargarNombreNegocioAsync();

        // Lógica de arranque: si el archivo JSON no existe, abrir Configuración (solo red); de lo contrario, ir al Login.
        if (!_configuracionService.EstaConfigurado)
        {
            NavegarAConfiguracion(esModoSoloRed: true);
        }
        else
        {
            NavegarALogin();
        }
    }

    private async Task CargarNombreNegocioAsync()
    {
        if (_configuracionRepository is null)
            return;

        try
        {
            var config = await _configuracionRepository.ObtenerConfiguracionAsync();
            NombreNegocio = string.IsNullOrWhiteSpace(config?.NombreNegocio)
                ? "Ferretería ERP"
                : config.NombreNegocio.Trim();
            ActualizarLogo(config?.LogoImagen);
        }
        catch
        {
            NombreNegocio = "Ferretería ERP";
            ActualizarLogo(null);
        }
    }

    public void ActualizarLogo(byte[]? logoBytes)
    {
        if (logoBytes is { Length: > 0 })
        {
            try
            {
                using var ms = new MemoryStream(logoBytes);
                LogoNegocioBitmap = new Bitmap(ms);
            }
            catch
            {
                LogoNegocioBitmap = null;
            }
        }
        else
        {
            LogoNegocioBitmap = null;
        }
    }

    public ObservableCollection<ProductoInventarioItem> ResultadosBusquedaGlobal { get; } = new();

    public bool MostrarSidebar =>
        UsuarioActual is not null &&
        CurrentViewModel is not LoginViewModel;

    /// <summary>
    /// PASO 33 (RBAC): Control centralizado de acceso administrativo (SuperAdmin, Administrador, Admin o Administrativo).
    /// Evalúa el rol real cargado en <see cref="UsuarioActual"/> o <see cref="SesionActual"/> sin distinguir mayúsculas/minúsculas.
    /// </summary>
    public bool TieneAccesoAdministrativo
    {
        get
        {
            var rolActual = (UsuarioActual?.Rol?.Nombre ?? SesionActual.Rol)?.Trim();
            if (string.IsNullOrWhiteSpace(rolActual))
                return false;

            return string.Equals(rolActual, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolActual, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolActual, "Admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rolActual, "Administrativo", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Expone si el usuario autenticado tiene rol de Administrador leyendo desde <see cref="SesionActual"/>.
    /// </summary>
    public bool EsAdmin => SesionActual.EsAdmin;

    /// <summary>
    /// Candado de seguridad para Inteligencia Gerencial (Dueño / SuperAdmin / Administrador).
    /// </summary>
    public bool EsSuperAdmin => SesionActual.EsSuperAdmin;

    /// <summary>
    /// Visibilidad del módulo de Reabastecimiento y Órdenes de Compra (Admin / SuperAdmin / Almacenero).
    /// </summary>
    public bool PuedeVerReabastecimiento => SesionActual.PuedeGestionarReabastecimiento;

    public event Action<string, string>? NotificacionExitoSolicitada;

    public bool EsVistaInicioActiva => CurrentViewModel is InicioViewModel;

    public bool EsVistaCajaActiva => CurrentViewModel is PosViewModel;

    public bool EsVistaCotizacionesActiva => CurrentViewModel is CotizacionViewModel;

    public bool EsVistaInventarioActiva => CurrentViewModel is InventarioViewModel;

    public bool EsVistaProveedoresActiva => CurrentViewModel is ProveedoresViewModel;

    public bool EsVistaLogisticaActiva => CurrentViewModel is LogisticaViewModel;

    public bool EsVistaEmpleadosActiva => CurrentViewModel is EmpleadosViewModel;

    public bool EsVistaCreditosActiva => CurrentViewModel is CreditosViewModel;

    public bool EsVistaPromocionesActiva => CurrentViewModel is PromocionesViewModel;

    public bool EsVistaReabastecimientoActiva => CurrentViewModel is ReabastecimientoViewModel;

    public bool EsVistaGerenciaActiva => CurrentViewModel is GerenciaViewModel;

    public bool EsVistaConfiguracionActiva => CurrentViewModel is ConfiguracionViewModel;

    public string NombreUsuarioActual => UsuarioActual?.NombreCompleto ?? string.Empty;

    public string DetalleRolSucursal => UsuarioActual is null
        ? string.Empty
        : $"{UsuarioActual.Rol?.Nombre ?? "Usuario"}  •  {UsuarioActual.Sucursal?.Nombre ?? "Sucursal"}";

    public string InicialesUsuarioActual
    {
        get
        {
            var nombre = UsuarioActual?.NombreCompleto?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                return "US";

            var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1)
                return partes[0].Substring(0, Math.Min(2, partes[0].Length)).ToUpperInvariant();

            return $"{char.ToUpperInvariant(partes[0][0])}{char.ToUpperInvariant(partes[1][0])}";
        }
    }

    partial void OnEsModoOscuroChanged(bool value)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }

    partial void OnTextoBusquedaGlobalChanged(string value)
    {
        _ = EjecutarBusquedaGlobalAsync(value);
    }

    partial void OnProductoBusquedaSeleccionadoChanged(ProductoInventarioItem? value)
    {
        if (value is null)
            return;

        IsBuscando = false;
        IrAInventario();
    }

    private async Task EjecutarBusquedaGlobalAsync(string? termino)
    {
        var filtro = termino?.Trim();
        if (string.IsNullOrWhiteSpace(filtro) || _productoRepository is null || _sesionService is null || !_sesionService.EstaAutenticado)
        {
            ResultadosBusquedaGlobal.Clear();
            IsBuscando = false;
            return;
        }

        try
        {
            var idSucursal = _sesionService.IdSucursalActual;
            var productos = await _productoRepository.ObtenerCatalogoConStockSucursalAsync(idSucursal, filtro);

            // Verificar que el texto no haya cambiado mientras consultábamos
            if (!string.Equals(TextoBusquedaGlobal?.Trim(), filtro, StringComparison.Ordinal))
                return;

            ResultadosBusquedaGlobal.Clear();
            foreach (var p in productos.Take(8))
            {
                var stock = p.Stocks.FirstOrDefault(s => s.IdSucursal == idSucursal)?.CantidadActual ?? 0;
                ResultadosBusquedaGlobal.Add(new ProductoInventarioItem
                {
                    Producto = p,
                    StockActual = stock
                });
            }

            IsBuscando = ResultadosBusquedaGlobal.Count > 0;
        }
        catch
        {
            IsBuscando = false;
        }
    }

    [RelayCommand]
    public void LimpiarBusquedaGlobal()
    {
        TextoBusquedaGlobal = string.Empty;
        ResultadosBusquedaGlobal.Clear();
        IsBuscando = false;
    }

    public void NavegarAConfiguracion(bool esModoSoloRed = false)
    {
        if (_serviceProvider is null || _configuracionService is null)
            return;

        var configuracionVm = _serviceProvider.GetRequiredService<ConfiguracionViewModel>();
        configuracionVm.Inicializar(esModoSoloRed);
        configuracionVm.NombreNegocioActualizado += nuevoNombre =>
        {
            NombreNegocio = string.IsNullOrWhiteSpace(nuevoNombre)
                ? "Ferretería ERP"
                : nuevoNombre.Trim();
        };
        configuracionVm.LogoNegocioActualizado += ActualizarLogo;
        configuracionVm.ConfiguracionGuardada += () =>
        {
            Program.InicializarBaseDeDatos(_serviceProvider);
            _ = CargarNombreNegocioAsync();
            if (UsuarioActual is not null)
                NavegarAInicio();
            else
                NavegarALogin();
        };

        if (_configuracionService.EstaConfigurado)
        {
            // Solo mostrar botón "Volver" cuando se accede desde la pantalla de Login (sin sesión iniciada)
            configuracionVm.PermitirCancelar = UsuarioActual is null;
            configuracionVm.CancelarSolicitado += () =>
            {
                if (UsuarioActual is not null)
                    NavegarAInicio();
                else
                    NavegarALogin();
            };
        }

        CurrentViewModel = configuracionVm;
    }

    public void NavegarALogin()
    {
        if (_serviceProvider is null)
            return;

        var loginVm = _serviceProvider.GetRequiredService<LoginViewModel>();
        loginVm.SesionIniciada += OnSesionIniciada;
        loginVm.AbrirConfiguracionSolicitado += () => NavegarAConfiguracion(esModoSoloRed: true);

        CurrentViewModel = loginVm;
    }

    private void OnSesionIniciada(Usuario usuario)
    {
        _sesionService?.EstablecerSesion(usuario);
        UsuarioActual = usuario;
        OnPropertyChanged(nameof(TieneAccesoAdministrativo));
        OnPropertyChanged(nameof(EsAdmin));
        OnPropertyChanged(nameof(EsSuperAdmin));
        OnPropertyChanged(nameof(PuedeVerReabastecimiento));

        NavegarAInicio();

        var sucursal = string.IsNullOrWhiteSpace(SesionActual.NombreSucursal)
            ? (usuario.Sucursal?.Nombre ?? "Sucursal Activa")
            : SesionActual.NombreSucursal;

        NotificacionExitoSolicitada?.Invoke(
            "Inicio de sesión exitoso",
            $"¡Bienvenido, {usuario.NombreCompleto}! Has iniciado sesión en {sucursal}.");
    }

    [RelayCommand]
    public void IrAInicio()
    {
        IsBuscando = false;
        if (_serviceProvider is null)
            return;

        if (CurrentViewModel is InicioViewModel inicioVm)
        {
            inicioVm.OnNavegarAPromociones = IrAPromociones;
            inicioVm.OnNavegarAReabastecimiento = IrAReabastecimiento;
            _ = inicioVm.CargarEstadisticasAsync();
            return;
        }

        CurrentViewModel = CrearInicioViewModel();
    }

    /// <summary>
    /// Alias de compatibilidad por si algún enlace legado invoca DashboardCommand.
    /// </summary>
    public IRelayCommand DashboardCommand => IrAInicioCommand;

    [RelayCommand]
    public void IrACaja()
    {
        IsBuscando = false;
        if (CurrentViewModel is PosViewModel)
            return;

        NavegarAPos();
    }

    [RelayCommand]
    public void IrACotizaciones()
    {
        IsBuscando = false;
        if (_serviceProvider is null || CurrentViewModel is CotizacionViewModel)
            return;

        CurrentViewModel = _serviceProvider.GetRequiredService<CotizacionViewModel>();
    }

    [RelayCommand]
    public void IrAInventario()
    {
        IsBuscando = false;
        if (_serviceProvider is null || CurrentViewModel is InventarioViewModel)
            return;

        CurrentViewModel = _serviceProvider.GetRequiredService<InventarioViewModel>();
    }

    [RelayCommand]
    public void IrAProveedores()
    {
        IsBuscando = false;
        if (!TieneAccesoAdministrativo || _serviceProvider is null || CurrentViewModel is ProveedoresViewModel)
            return;

        CurrentViewModel = _serviceProvider.GetRequiredService<ProveedoresViewModel>();
    }

    [RelayCommand]
    public void IrALogistica()
    {
        IsBuscando = false;
        if (_serviceProvider is null || CurrentViewModel is LogisticaViewModel)
            return;

        CurrentViewModel = _serviceProvider.GetRequiredService<LogisticaViewModel>();
    }

    [RelayCommand]
    public void IrAEmpleados()
    {
        IsBuscando = false;
        if (!TieneAccesoAdministrativo || _serviceProvider is null || CurrentViewModel is EmpleadosViewModel)
            return;

        var empleadosVm = _serviceProvider.GetRequiredService<EmpleadosViewModel>();
        empleadosVm.RolSesionActualActualizado += OnRolSesionActualActualizado;
        CurrentViewModel = empleadosVm;
    }

    private void OnRolSesionActualActualizado(Usuario usuarioActualizado)
    {
        if (UsuarioActual is null || UsuarioActual.Id != usuarioActualizado.Id)
            return;

        if (usuarioActualizado.Rol is not null)
        {
            UsuarioActual.Rol = usuarioActualizado.Rol;
            UsuarioActual.IdRol = usuarioActualizado.IdRol;
            SesionActual.Rol = usuarioActualizado.Rol.Nombre;
        }

        UsuarioActual.NombreCompleto = usuarioActualizado.NombreCompleto;
        SesionActual.NombreCompleto = usuarioActualizado.NombreCompleto;

        OnPropertyChanged(nameof(TieneAccesoAdministrativo));
        OnPropertyChanged(nameof(EsAdmin));
        OnPropertyChanged(nameof(EsSuperAdmin));
        OnPropertyChanged(nameof(PuedeVerReabastecimiento));
        OnPropertyChanged(nameof(NombreUsuarioActual));
        OnPropertyChanged(nameof(DetalleRolSucursal));
        OnPropertyChanged(nameof(InicialesUsuarioActual));
    }

    [RelayCommand]
    public void IrACreditos()
    {
        IsBuscando = false;
        if (_serviceProvider is null || CurrentViewModel is CreditosViewModel)
            return;

        CurrentViewModel = _serviceProvider.GetRequiredService<CreditosViewModel>();
    }

    [RelayCommand]
    public void IrAPromociones()
    {
        IsBuscando = false;
        if (_serviceProvider is null)
            return;

        if (CurrentViewModel is PromocionesViewModel promoVm)
        {
            _ = promoVm.InicializarAsync();
            return;
        }

        CurrentViewModel = _serviceProvider.GetRequiredService<PromocionesViewModel>();
    }

    [RelayCommand]
    public void IrAReabastecimiento()
    {
        IsBuscando = false;
        if (!PuedeVerReabastecimiento || _serviceProvider is null)
            return;

        if (CurrentViewModel is ReabastecimientoViewModel reabVm)
        {
            _ = reabVm.InicializarAsync();
            return;
        }

        CurrentViewModel = _serviceProvider.GetRequiredService<ReabastecimientoViewModel>();
    }

    [RelayCommand]
    public void IrAGerencia()
    {
        IsBuscando = false;
        if (!TieneAccesoAdministrativo || _serviceProvider is null)
            return;

        if (CurrentViewModel is GerenciaViewModel gerenciaVm)
        {
            _ = gerenciaVm.AplicarRangoAsync();
            return;
        }

        CurrentViewModel = _serviceProvider.GetRequiredService<GerenciaViewModel>();
    }

    [RelayCommand]
    public void IrAConfiguracion()
    {
        IsBuscando = false;
        // PASO 33 (RBAC): Todos los roles acceden a Configuración; los no administrativos en modo Solo Red
        NavegarAConfiguracion(esModoSoloRed: !TieneAccesoAdministrativo);
    }

    private InicioViewModel CrearInicioViewModel()
    {
        var inicioVm = _serviceProvider!.GetRequiredService<InicioViewModel>();
        inicioVm.OnNavegarAPromociones = IrAPromociones;
        inicioVm.OnNavegarAReabastecimiento = IrAReabastecimiento;
        return inicioVm;
    }

    private void NavegarAInicio()
    {
        if (_serviceProvider is null)
            return;

        try
        {
            CurrentViewModel = CrearInicioViewModel();
        }
        catch (Exception ex)
        {
            // Nunca se traga la excepción: queda registrada y se muestra en pantalla como mensaje de error
            System.Diagnostics.Debug.WriteLine($"[Navegación] No se pudo crear InicioViewModel: {ex}");
            CurrentViewModel = new ErrorNavegacionViewModel($"No se pudo abrir Inicio: {(ex.InnerException ?? ex).Message}");
        }
    }

    private void NavegarAPos()
    {
        if (_serviceProvider is null)
            return;

        CurrentViewModel = _serviceProvider.GetRequiredService<PosViewModel>();
    }

    [RelayCommand]
    public void CerrarSesion()
    {
        IsBuscando = false;
        TextoBusquedaGlobal = string.Empty;
        ResultadosBusquedaGlobal.Clear();
        _sesionService?.CerrarSesion();
        UsuarioActual = null;
        NavegarALogin();
    }
}
