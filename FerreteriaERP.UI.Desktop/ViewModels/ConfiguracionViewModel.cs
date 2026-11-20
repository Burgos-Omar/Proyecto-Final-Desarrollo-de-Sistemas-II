using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Configuration;
using FerreteriaERP.Core.Enums;
using FerreteriaERP.Core.Interfaces;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class ConfiguracionViewModel : ViewModelBase
{
    private readonly IConfiguracionConexionService _configuracionService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsSucursal))]
    [NotifyPropertyChangedFor(nameof(ResumenConexion))]
    private bool _esCentral = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResumenConexion))]
    private string _ipVpn = string.Empty;

    [ObservableProperty]
    private string _passwordBd = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private string? _mensajeEstado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private bool _esExito;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CamposHabilitados))]
    [NotifyPropertyChangedFor(nameof(TextoBotonGuardar))]
    [NotifyCanExecuteChangedFor(nameof(GuardarYProbarConexionCommand))]
    private bool _estaProbando;

    [ObservableProperty]
    private bool _permitirCancelar;

    public ConfiguracionViewModel(IConfiguracionConexionService configuracionService)
    {
        _configuracionService = configuracionService;

        var actual = _configuracionService.Actual;
        _esCentral = actual.Modo == ModoOperacion.Central;
        _ipVpn = actual.Modo == ModoOperacion.Sucursal ? actual.Servidor : string.Empty;
        _passwordBd = actual.Password;
    }

    public bool EsSucursal
    {
        get => !EsCentral;
        set
        {
            if (EsCentral == value)
            {
                EsCentral = !value;
            }
        }
    }

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsExito;

    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsExito;

    public bool CamposHabilitados => !EstaProbando;

    public string TextoBotonGuardar => EstaProbando
        ? "Probando conexión..."
        : "Guardar y Probar Conexión";

    public string ResumenConexion => EsCentral
        ? "Host: localhost  •  Puerto: 5432  •  Usuario: postgres  •  BD: FerreteriaDSS"
        : $"Host: {(string.IsNullOrWhiteSpace(IpVpn) ? "IP VPN pendiente" : IpVpn.Trim())}  •  Puerto: 5432  •  Usuario: postgres  •  BD: FerreteriaDSS";

    public event Action? ConfiguracionGuardada;
    public event Action? CancelarSolicitado;

    partial void OnEsCentralChanged(bool value) => LimpiarMensaje();

    partial void OnIpVpnChanged(string value) => LimpiarMensaje();

    partial void OnPasswordBdChanged(string value) => LimpiarMensaje();

    private bool PuedeGuardar() => !EstaProbando;

    [RelayCommand(CanExecute = nameof(PuedeGuardar))]
    private async Task GuardarYProbarConexionAsync()
    {
        LimpiarMensaje();

        if (EsSucursal && string.IsNullOrWhiteSpace(IpVpn))
        {
            MostrarMensajeError("Debe ingresar la IP de la VPN del servidor central.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PasswordBd))
        {
            MostrarMensajeError("Debe ingresar la contraseña de la base de datos PostgreSQL.");
            return;
        }

        var actual = _configuracionService.Actual;
        var nuevaConfiguracion = new ConfiguracionConexion
        {
            Modo = EsCentral ? ModoOperacion.Central : ModoOperacion.Sucursal,
            Servidor = EsCentral ? string.Empty : IpVpn.Trim(),
            Puerto = actual.Puerto > 0 ? actual.Puerto : 5432,
            BaseDatos = string.IsNullOrWhiteSpace(actual.BaseDatos) ? "FerreteriaDSS" : actual.BaseDatos,
            Usuario = string.IsNullOrWhiteSpace(actual.Usuario) ? "postgres" : actual.Usuario,
            Password = PasswordBd
        };

        EstaProbando = true;
        try
        {
            var (exitoso, error) = await _configuracionService.ProbarConexionAsync(nuevaConfiguracion);
            if (!exitoso)
            {
                MostrarMensajeError($"No se pudo conectar a la base de datos: {error}");
                return;
            }

            _configuracionService.Guardar(nuevaConfiguracion);
            MostrarMensajeExito("¡Conexión verificada y configuración guardada exitosamente!");

            await Task.Delay(500);
            ConfiguracionGuardada?.Invoke();
        }
        finally
        {
            EstaProbando = false;
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        CancelarSolicitado?.Invoke();
    }

    private void MostrarMensajeError(string mensaje)
    {
        EsExito = false;
        MensajeEstado = mensaje;
    }

    private void MostrarMensajeExito(string mensaje)
    {
        EsExito = true;
        MensajeEstado = mensaje;
    }

    private void LimpiarMensaje()
    {
        MensajeEstado = null;
    }
}

