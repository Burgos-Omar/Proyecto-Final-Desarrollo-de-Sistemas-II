using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Configuration;
using FerreteriaERP.Core.Enums;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Services;
using FerreteriaERP.UI.Desktop.Views;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class ConfiguracionViewModel : ViewModelBase
{
    private readonly IConfiguracionConexionService _configuracionService;
    private readonly BackupService _backupService;
    private readonly IConfiguracionRepository _configuracionRepository;

    private byte[]? _qrBytesActuales;
    private byte[]? _logoBytesActuales;

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
    [NotifyCanExecuteChangedFor(nameof(GenerarRespaldoCommand))]
    private bool _estaProbando;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CamposHabilitados))]
    [NotifyPropertyChangedFor(nameof(TextoBotonRespaldo))]
    [NotifyCanExecuteChangedFor(nameof(GuardarYProbarConexionCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerarRespaldoCommand))]
    private bool _estaGenerandoRespaldo;

    // Configuración de Cobro por QR Bancario (PASO 31)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneQrConfigurado))]
    [NotifyPropertyChangedFor(nameof(SinQrConfigurado))]
    private Bitmap? _qrPreviewBitmap;

    [ObservableProperty]
    private string _bancoQr = string.Empty;

    [ObservableProperty]
    private string _titularQr = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CamposHabilitados))]
    private bool _estaGuardandoQr;

    // Datos Fiscales del Negocio (Razón Social y NIT)
    [ObservableProperty]
    private string _nombreNegocioForm = string.Empty;

    [ObservableProperty]
    private string _nitNegocioForm = string.Empty;

    [ObservableProperty]
    private string _direccionForm = string.Empty;

    [ObservableProperty]
    private string _telefonoForm = string.Empty;

    [ObservableProperty]
    private string _mensajePieReciboForm = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CamposHabilitados))]
    private bool _estaGuardandoDatosFiscales;

    // Identidad Visual: Logo del Negocio (PASO 31c)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneLogoConfigurado))]
    [NotifyPropertyChangedFor(nameof(SinLogoConfigurado))]
    private Bitmap? _logoPreviewBitmap;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CamposHabilitados))]
    private bool _estaGuardandoLogo;

    [ObservableProperty]
    private bool _permitirCancelar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarConfiguracionCompleta))]
    private bool _esModoSoloRed;

    public ConfiguracionViewModel(
        IConfiguracionConexionService configuracionService,
        BackupService backupService,
        IConfiguracionRepository configuracionRepository)
    {
        _configuracionService = configuracionService;
        _backupService = backupService;
        _configuracionRepository = configuracionRepository;

        var actual = _configuracionService.Actual;
        _esCentral = actual.Modo == ModoOperacion.Central;
        _ipVpn = actual.Modo == ModoOperacion.Sucursal ? actual.Servidor : string.Empty;
        _passwordBd = actual.Password;
    }

    /// <summary>
    /// PASO 32: Inicializa la vista en modo solo red (desde Login) o completo (desde Dashboard).
    /// Evita consultas a PostgreSQL (QR, NIT, Logo) cuando solo se está configurando la red.
    /// </summary>
    public void Inicializar(bool esModoSoloRed)
    {
        EsModoSoloRed = esModoSoloRed;
        if (!esModoSoloRed)
        {
            _ = CargarQrGuardadoAsync();
        }
    }

    public bool MostrarConfiguracionCompleta => !EsModoSoloRed;

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

    public bool CamposHabilitados =>
        !EstaProbando && !EstaGenerandoRespaldo && !EstaGuardandoQr && !EstaGuardandoDatosFiscales && !EstaGuardandoLogo;

    public bool TieneQrConfigurado => QrPreviewBitmap is not null;

    public bool SinQrConfigurado => QrPreviewBitmap is null;

    public bool TieneLogoConfigurado => LogoPreviewBitmap is not null;

    public bool SinLogoConfigurado => LogoPreviewBitmap is null;

    public string TextoBotonGuardar => EstaProbando
        ? "Probando conexión..."
        : "Guardar y Probar Conexión";

    public string TextoBotonRespaldo => EstaGenerandoRespaldo
        ? "Generando Copia de Seguridad..."
        : "Generar Copia de Seguridad";

    public string ResumenConexion => EsCentral
        ? "Host: localhost  •  Puerto: 5432  •  Usuario: postgres  •  BD: FerreteriaDSS"
        : $"Host: {(string.IsNullOrWhiteSpace(IpVpn) ? "IP VPN pendiente" : IpVpn.Trim())}  •  Puerto: 5432  •  Usuario: postgres  •  BD: FerreteriaDSS";

    public event Action? ConfiguracionGuardada;
    public event Action? CancelarSolicitado;
    public event Action<string>? NombreNegocioActualizado;
    public event Action<byte[]?>? LogoNegocioActualizado;

    partial void OnEsCentralChanged(bool value) => LimpiarMensaje();

    partial void OnIpVpnChanged(string value) => LimpiarMensaje();

    partial void OnPasswordBdChanged(string value) => LimpiarMensaje();

    private bool PuedeEjecutarAccion() => !EstaProbando && !EstaGenerandoRespaldo;

    [RelayCommand(CanExecute = nameof(PuedeEjecutarAccion))]
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

    /// <summary>
    /// PASO 25: Abre el selector de archivos y genera un respaldo completo (.backup) con pg_dump.
    /// </summary>
    [RelayCommand(CanExecute = nameof(PuedeEjecutarAccion))]
    private async Task GenerarRespaldoAsync()
    {
        LimpiarMensaje();

        var topLevel = ObtenerTopLevelActivo();
        if (topLevel?.StorageProvider is null)
            return;

        var archivoDestino = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar Copia de Seguridad de PostgreSQL",
            SuggestedFileName = $"Ferreteria_Backup_{DateTime.Now:yyyyMM}.backup",
            DefaultExtension = "backup",
            ShowOverwritePrompt = true,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Respaldo PostgreSQL (.backup)")
                {
                    Patterns = new[] { "*.backup" }
                }
            }
        });

        if (archivoDestino is null)
            return;

        string rutaLocal = archivoDestino.TryGetLocalPath()
                           ?? archivoDestino.Path.LocalPath;

        if (!rutaLocal.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
        {
            rutaLocal += ".backup";
        }

        EstaGenerandoRespaldo = true;
        try
        {
            string connectionString = _configuracionService.ConstruirCadenaConexion();
            await _backupService.GenerarRespaldoAsync(rutaLocal, connectionString);

            MostrarMensajeExito($"Copia de seguridad guardada en: {rutaLocal}");
            MostrarToast("Copia de seguridad generada correctamente", NotificationType.Success);
        }
        catch (Exception)
        {
            const string mensajeError = "Error al generar respaldo. Verifique que PostgreSQL esté instalado.";
            MostrarMensajeError(mensajeError);
            MostrarToast(mensajeError, NotificationType.Error);
        }
        finally
        {
            EstaGenerandoRespaldo = false;
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        CancelarSolicitado?.Invoke();
    }

    private async Task CargarQrGuardadoAsync()
    {
        try
        {
            var config = await _configuracionRepository.ObtenerConfiguracionAsync();
            NombreNegocioForm = config.NombreNegocio ?? string.Empty;
            NitNegocioForm = config.NitNegocio ?? string.Empty;
            DireccionForm = config.Direccion ?? string.Empty;
            TelefonoForm = config.Telefono ?? string.Empty;
            MensajePieReciboForm = config.MensajePieRecibo ?? string.Empty;
            BancoQr = config.BancoQr ?? string.Empty;
            TitularQr = config.TitularQr ?? string.Empty;
            _qrBytesActuales = config.QrPagoImagen;
            _logoBytesActuales = config.LogoImagen;

            if (_qrBytesActuales is { Length: > 0 })
            {
                using var ms = new MemoryStream(_qrBytesActuales);
                QrPreviewBitmap = new Bitmap(ms);
            }
            else
            {
                QrPreviewBitmap = null;
            }

            if (_logoBytesActuales is { Length: > 0 })
            {
                using var msLogo = new MemoryStream(_logoBytesActuales);
                LogoPreviewBitmap = new Bitmap(msLogo);
            }
            else
            {
                LogoPreviewBitmap = null;
            }
        }
        catch
        {
            // No bloquear la pantalla de configuración si aún no hay conexión a la BD
        }
    }

    /// <summary>
    /// PASO 31: Abre StorageProvider.OpenFilePickerAsync para seleccionar la imagen del QR bancario.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarImagenQrAsync()
    {
        LimpiarMensaje();

        var topLevel = ObtenerTopLevelActivo();
        if (topLevel?.StorageProvider is null)
            return;

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar Imagen de QR Bancario",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imágenes QR")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" },
                    MimeTypes = new[] { "image/png", "image/jpeg", "image/webp" }
                }
            }
        });

        var archivo = archivos?.FirstOrDefault();
        if (archivo is null)
            return;

        try
        {
            await using var stream = await archivo.OpenReadAsync();
            using var msLectura = new MemoryStream();
            await stream.CopyToAsync(msLectura);
            var bytes = msLectura.ToArray();

            if (bytes.Length == 0)
            {
                MostrarMensajeError("El archivo seleccionado está vacío.");
                return;
            }

            _qrBytesActuales = bytes;
            using var msBitmap = new MemoryStream(bytes);
            QrPreviewBitmap = new Bitmap(msBitmap);
            MostrarMensajeExito("Imagen QR cargada en vista previa. Presione 'Guardar QR' para aplicarla en Caja.");
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"No se pudo cargar la imagen QR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GuardarQrAsync()
    {
        LimpiarMensaje();

        if (_qrBytesActuales is null || _qrBytesActuales.Length == 0)
        {
            MostrarMensajeError("Seleccione primero una imagen QR antes de guardar.");
            return;
        }

        EstaGuardandoQr = true;
        try
        {
            await _configuracionRepository.GuardarQrPagoAsync(_qrBytesActuales, BancoQr, TitularQr);
            const string mensaje = "QR bancario guardado correctamente y disponible en Caja (POS).";
            MostrarMensajeExito(mensaje);
            MostrarToast(mensaje, NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al guardar el QR bancario: {ex.Message}");
            MostrarToast("Error al guardar el QR bancario", NotificationType.Error);
        }
        finally
        {
            EstaGuardandoQr = false;
        }
    }

    [RelayCommand]
    private async Task EliminarQrAsync()
    {
        LimpiarMensaje();
        EstaGuardandoQr = true;
        try
        {
            await _configuracionRepository.GuardarQrPagoAsync(null, null, null);
            _qrBytesActuales = null;
            QrPreviewBitmap = null;
            BancoQr = string.Empty;
            TitularQr = string.Empty;

            const string mensaje = "QR bancario eliminado correctamente.";
            MostrarMensajeExito(mensaje);
            MostrarToast(mensaje, NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al eliminar el QR bancario: {ex.Message}");
        }
        finally
        {
            EstaGuardandoQr = false;
        }
    }

    [RelayCommand]
    private async Task GuardarDatosFiscalesAsync()
    {
        LimpiarMensaje();

        var nit = NitNegocioForm?.Trim();
        if (!string.IsNullOrEmpty(nit) && nit.Length > 30)
        {
            MostrarMensajeError("El NIT del negocio no puede exceder los 30 caracteres.");
            return;
        }

        EstaGuardandoDatosFiscales = true;
        try
        {
            var actualizado = await _configuracionRepository.GuardarDatosFiscalesAsync(
                NombreNegocioForm,
                nit,
                DireccionForm,
                TelefonoForm,
                MensajePieReciboForm);
            var nombreFinal = string.IsNullOrWhiteSpace(actualizado.NombreNegocio)
                ? "Ferretería ERP"
                : actualizado.NombreNegocio.Trim();
            NombreNegocioForm = nombreFinal;
            NombreNegocioActualizado?.Invoke(nombreFinal);

            const string mensaje = "Identidad, datos fiscales, contacto y pie de recibo guardados correctamente.";
            MostrarMensajeExito(mensaje);
            MostrarToast(mensaje, NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al guardar los datos fiscales: {ex.Message}");
            MostrarToast("Error al guardar datos fiscales", NotificationType.Error);
        }
        finally
        {
            EstaGuardandoDatosFiscales = false;
        }
    }

    /// <summary>
    /// PASO 31c: Abre StorageProvider.OpenFilePickerAsync para seleccionar el logo corporativo del negocio.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarLogoAsync()
    {
        LimpiarMensaje();

        var topLevel = ObtenerTopLevelActivo();
        if (topLevel?.StorageProvider is null)
            return;

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar Logo del Negocio",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imágenes de Logo")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" },
                    MimeTypes = new[] { "image/png", "image/jpeg", "image/webp" }
                }
            }
        });

        var archivo = archivos?.FirstOrDefault();
        if (archivo is null)
            return;

        try
        {
            await using var stream = await archivo.OpenReadAsync();
            using var msLectura = new MemoryStream();
            await stream.CopyToAsync(msLectura);
            var bytes = msLectura.ToArray();

            if (bytes.Length == 0)
            {
                MostrarMensajeError("El archivo de logo seleccionado está vacío.");
                return;
            }

            _logoBytesActuales = bytes;
            using var msBitmap = new MemoryStream(bytes);
            LogoPreviewBitmap = new Bitmap(msBitmap);
            MostrarMensajeExito("Logo cargado en vista previa. Presione 'Guardar Logo' para aplicarlo en los documentos PDF.");
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"No se pudo cargar la imagen del logo: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GuardarLogoAsync()
    {
        LimpiarMensaje();

        if (_logoBytesActuales is null || _logoBytesActuales.Length == 0)
        {
            MostrarMensajeError("Seleccione primero una imagen de logo antes de guardar.");
            return;
        }

        EstaGuardandoLogo = true;
        try
        {
            var actualizado = await _configuracionRepository.GuardarLogoAsync(_logoBytesActuales, NombreNegocioForm);
            if (!string.IsNullOrWhiteSpace(actualizado.NombreNegocio))
            {
                NombreNegocioActualizado?.Invoke(actualizado.NombreNegocio.Trim());
            }
            LogoNegocioActualizado?.Invoke(_logoBytesActuales);

            const string mensaje = "Logo del negocio guardado correctamente y activo en todos los documentos PDF.";
            MostrarMensajeExito(mensaje);
            MostrarToast(mensaje, NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al guardar el logo del negocio: {ex.Message}");
            MostrarToast("Error al guardar el logo", NotificationType.Error);
        }
        finally
        {
            EstaGuardandoLogo = false;
        }
    }

    [RelayCommand]
    private async Task EliminarLogoAsync()
    {
        LimpiarMensaje();
        EstaGuardandoLogo = true;
        try
        {
            await _configuracionRepository.GuardarLogoAsync(null);
            _logoBytesActuales = null;
            LogoPreviewBitmap = null;
            LogoNegocioActualizado?.Invoke(null);

            const string mensaje = "Logo del negocio eliminado correctamente.";
            MostrarMensajeExito(mensaje);
            MostrarToast(mensaje, NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al eliminar el logo del negocio: {ex.Message}");
        }
        finally
        {
            EstaGuardandoLogo = false;
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
                "Seguridad y Respaldos",
                mensaje,
                tipo,
                TimeSpan.FromSeconds(5)));
        });
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
