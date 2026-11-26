using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Security;
using FerreteriaERP.Core.Services;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IConfiguracionRepository? _configuracionRepository;

    [ObservableProperty]
    private string _nombreNegocio = "Ferretería ERP";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneLogoPersonalizado))]
    private Bitmap? _logoNegocioBitmap;

    public bool TieneLogoPersonalizado => LogoNegocioBitmap is not null;

    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneError))]
    private string? _mensajeError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CamposHabilitados))]
    [NotifyPropertyChangedFor(nameof(TextoBotonLogin))]
    [NotifyCanExecuteChangedFor(nameof(IniciarSesionCommand))]
    private bool _isBuscando;

    public LoginViewModel(
        IUsuarioRepository usuarioRepository,
        IPasswordHasherService passwordHasher,
        IConfiguracionRepository? configuracionRepository = null)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _configuracionRepository = configuracionRepository;

        _ = CargarNombreNegocioAsync();
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

            if (config?.LogoImagen is { Length: > 0 })
            {
                using var ms = new MemoryStream(config.LogoImagen);
                LogoNegocioBitmap = new Bitmap(ms);
            }
            else
            {
                LogoNegocioBitmap = null;
            }
        }
        catch
        {
            NombreNegocio = "Ferretería ERP";
            LogoNegocioBitmap = null;
        }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public bool CamposHabilitados => !IsBuscando;

    public string TextoBotonLogin => IsBuscando
        ? "Verificando..."
        : "Ingresar";

    public event Action<Usuario>? SesionIniciada;
    public event Action? AbrirConfiguracionSolicitado;

    partial void OnUsuarioChanged(string value) => MensajeError = null;

    partial void OnPasswordChanged(string value) => MensajeError = null;

    private bool PuedeIniciarSesion() => !IsBuscando;

    [RelayCommand(CanExecute = nameof(PuedeIniciarSesion))]
    private async Task IniciarSesionAsync()
    {
        MensajeError = null;

        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            MensajeError = "Ingrese su usuario (C.I.) y contraseña.";
            return;
        }

        IsBuscando = true;
        try
        {
            var usuarioEncontrado = await _usuarioRepository.ObtenerPorCiAsync(Usuario.Trim());
            if (usuarioEncontrado is null)
            {
                MensajeError = "Usuario o contraseña incorrectos.";
                return;
            }

            var passwordTexto = Password;
            var esValida = await Task.Run(() =>
                _passwordHasher.VerifyPassword(passwordTexto, usuarioEncontrado.PasswordHash));

            if (!esValida)
            {
                MensajeError = "Usuario o contraseña incorrectos.";
                return;
            }

            if (!string.Equals(usuarioEncontrado.Estado, EstadosUsuario.Activo, StringComparison.OrdinalIgnoreCase))
            {
                MensajeError = "Su cuenta se encuentra inhabilitada. Contacte al administrador.";
                return;
            }

            // PASO 32: Si el usuario aún tenía un hash heredado (ej. BCrypt), actualizarlo transparentemente a PBKDF2-SHA256
            if (!PasswordHasher.IsHashed(usuarioEncontrado.PasswordHash))
            {
                try
                {
                    usuarioEncontrado.PasswordHash = await Task.Run(() => PasswordHasher.HashPassword(passwordTexto));
                    await _usuarioRepository.ActualizarAsync(usuarioEncontrado);
                }
                catch
                {
                    // No bloquear el inicio de sesión si falla la actualización oportunista del hash
                }
            }

            // Llenar gestor estático de sesión antes de notificar el éxito del login
            SesionActual.Establecer(usuarioEncontrado);

            Password = string.Empty;
            SesionIniciada?.Invoke(usuarioEncontrado);
        }
        catch (Exception)
        {
            MensajeError = "No se pudo conectar con la base de datos. Verifique la red o la configuración de conexión.";
        }
        finally
        {
            IsBuscando = false;
        }
    }

    [RelayCommand]
    private void AbrirConfiguracion()
    {
        AbrirConfiguracionSolicitado?.Invoke();
    }
}
