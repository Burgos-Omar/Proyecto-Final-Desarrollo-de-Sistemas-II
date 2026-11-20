using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
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
    private readonly ISesionService? _sesionService;
    private readonly IProductoRepository? _productoRepository;
    private readonly IServiceProvider? _serviceProvider;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarSidebar))]
    [NotifyPropertyChangedFor(nameof(EsVistaInicioActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaCajaActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaInventarioActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaProveedoresActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaLogisticaActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaEmpleadosActiva))]
    [NotifyPropertyChangedFor(nameof(EsVistaCreditosActiva))]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarSidebar))]
    [NotifyPropertyChangedFor(nameof(EsAdmin))]
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

    /// <summary>
    /// Constructor sin parámetros requerido por el previsualizador (designer) de Avalonia.
    /// </summary>
    public MainViewModel()
    {
    }

    [ActivatorUtilitiesConstructor]
    public MainViewModel(
        IConfiguracionConexionService configuracionService,
        ISesionService sesionService,
        IProductoRepository productoRepository,
        IServiceProvider serviceProvider)
    {
        _configuracionService = configuracionService;
        _sesionService = sesionService;
        _productoRepository = productoRepository;
        _serviceProvider = serviceProvider;

        // ============================================================
        // VERSIÓN BASE — BYPASS DE LOGIN (propósito académico)
        // Se inyecta una sesión mock para que EF Core no falle al
        // consultar IdSucursal u otros campos de sesión.
        // Para restaurar el flujo normal: eliminar este bloque y
        // descomentar la lógica original de Login/Configuración.
        // ============================================================
        var usuarioMock = new Usuario
        {
            Id              = 1,
            NombreCompleto  = "Administrador Demo",
            Ci              = "00000000",
            IdRol           = 1,
            Rol             = new Rol { Id = 1, Nombre = "Administrador" },
            IdSucursal      = 1,
            Sucursal        = new Sucursal { Id = 1, Nombre = "Sucursal Central", EsCentral = true },
            Estado          = "Activo",
            FechaInicio     = new System.DateTime(2024, 1, 1),
        };

        // Establece la sesión estática para que SesionActual.EsAdmin,
        // SesionActual.IdSucursal, etc. devuelvan valores válidos.
        SesionActual.Establecer(usuarioMock);

        // Establece también la sesión en el servicio inyectado.
        _sesionService.EstablecerSesion(usuarioMock);

        // Rellena UsuarioActual para que el Sidebar calcule MostrarSidebar = true.
        UsuarioActual = usuarioMock;

        // Navega directamente al Dashboard sin pasar por Login.
        NavegarAInicio();

        // ============================================================
        // LÓGICA ORIGINAL (comentada — descomenta para restaurar)
        // ============================================================
        // if (!_configuracionService.EstaConfigurado)
        // {
        //     NavegarAConfiguracion();
        // }
        // else
        // {
        //     NavegarALogin();
        // }
    }

    public ObservableCollection<ProductoInventarioItem> ResultadosBusquedaGlobal { get; } = new();

    public bool MostrarSidebar =>
        UsuarioActual is not null &&
        CurrentViewModel is not LoginViewModel &&
        CurrentViewModel is not ConfiguracionViewModel;

    /// <summary>
    /// Expone si el usuario autenticado tiene rol de Administrador leyendo desde <see cref="SesionActual"/>.
    /// </summary>
    public bool EsAdmin => SesionActual.EsAdmin;

    public event Action<string, string>? NotificacionExitoSolicitada;

    public bool EsVistaInicioActiva => CurrentViewModel is InicioViewModel;

    public bool EsVistaCajaActiva => CurrentViewModel is PosViewModel;

    public bool EsVistaInventarioActiva => CurrentViewModel is InventarioViewModel;

    public bool EsVistaProveedoresActiva => CurrentViewModel is ProveedoresViewModel;

    public bool EsVistaLogisticaActiva => CurrentViewModel is LogisticaViewModel;

    public bool EsVistaEmpleadosActiva => CurrentViewModel is EmpleadosViewModel;

    public bool EsVistaCreditosActiva => CurrentViewModel is CreditosViewModel;

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

    public void NavegarAConfiguracion()
    {
        if (_serviceProvider is null || _configuracionService is null)
            return;

        var configuracionVm = _serviceProvider.GetRequiredService<ConfiguracionViewModel>();
        configuracionVm.ConfiguracionGuardada += () =>
        {
            Program.InicializarBaseDeDatos(_serviceProvider);
            if (UsuarioActual is not null)
                NavegarAInicio();
            else
                NavegarALogin();
        };

        if (_configuracionService.EstaConfigurado)
        {
            configuracionVm.PermitirCancelar = true;
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
        loginVm.AbrirConfiguracionSolicitado += NavegarAConfiguracion;

        CurrentViewModel = loginVm;
    }

    private void OnSesionIniciada(Usuario usuario)
    {
        _sesionService?.EstablecerSesion(usuario);
        UsuarioActual = usuario;
        OnPropertyChanged(nameof(EsAdmin));

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
            _ = inicioVm.CargarEstadisticasAsync();
            return;
        }

        CurrentViewModel = _serviceProvider.GetRequiredService<InicioViewModel>();
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
        if (_serviceProvider is null || CurrentViewModel is ProveedoresViewModel)
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
        if (_serviceProvider is null || CurrentViewModel is EmpleadosViewModel)
            return;

        CurrentViewModel = _serviceProvider.GetRequiredService<EmpleadosViewModel>();
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
    public void IrAConfiguracion()
    {
        IsBuscando = false;
        if (!EsAdmin)
            return;

        NavegarAConfiguracion();
    }

    private void NavegarAInicio()
    {
        if (_serviceProvider is null)
            return;

        try
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<InicioViewModel>();
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
