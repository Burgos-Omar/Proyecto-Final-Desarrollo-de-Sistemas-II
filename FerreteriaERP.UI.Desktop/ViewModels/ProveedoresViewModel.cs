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
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class ProveedoresViewModel : ViewModelBase
{
    private static readonly string CarpetaDocumentosLocales = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FerreteriaERP",
        "Documentos");

    private readonly IRepository<Proveedor> _proveedorRepository;
    private List<Proveedor> _todosLosProveedores = new();
    private int? _idProveedorEditando;

    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    private Proveedor? _proveedorSeleccionado;

    [ObservableProperty]
    private bool _formularioVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloFormulario))]
    [NotifyPropertyChangedFor(nameof(TextoBotonGuardar))]
    private bool _esModoEdicion;

    // =========================================================
    // SECCIÓN 1: Datos Legales y Contacto
    // =========================================================
    [ObservableProperty]
    private string _nombreEmpresaForm = string.Empty;

    [ObservableProperty]
    private string _nombreComercialForm = string.Empty;

    [ObservableProperty]
    private string _nitForm = string.Empty;

    [ObservableProperty]
    private string _licenciaFuncionamientoForm = string.Empty;

    [ObservableProperty]
    private string _ciContactoForm = string.Empty;

    [ObservableProperty]
    private string _telefonosContactoForm = string.Empty;

    [ObservableProperty]
    private string _emailInstitucionalForm = string.Empty;

    [ObservableProperty]
    private string _direccionAlmacenForm = string.Empty;

    // =========================================================
    // SECCIÓN 2: Términos Comerciales
    // =========================================================
    [ObservableProperty]
    private string _monedaFacturacionSeleccionada = "BOB";

    [ObservableProperty]
    private string _formasPagoAceptadasForm = string.Empty;

    [ObservableProperty]
    private string _tiemposEntregaForm = string.Empty;

    [ObservableProperty]
    private string _politicaFletesForm = string.Empty;

    [ObservableProperty]
    private string _politicaGarantiaYCambiosForm = string.Empty;

    // =========================================================
    // SECCIÓN 3: Documentación Adjunta (PDF / DOCX)
    // =========================================================
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneDocumentoEnFormulario))]
    [NotifyPropertyChangedFor(nameof(NombreDocumentoFormTexto))]
    private string? _rutaDocumentoAdjuntoForm;

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
    private int _totalProveedoresCount;

    public ProveedoresViewModel(IRepository<Proveedor> proveedorRepository)
    {
        _proveedorRepository = proveedorRepository;
        _ = CargarProveedoresAsync();
    }

    public ObservableCollection<Proveedor> Proveedores { get; } = new();

    public IReadOnlyList<string> MonedasDisponibles { get; } = new[]
    {
        "BOB",
        "USD",
        "EUR",
        "MXN",
        "ARS",
        "CLP",
        "COP",
        "BRL",
        "PEN"
    };

    public bool SinResultados => Proveedores.Count == 0;

    public bool ControlesHabilitados => !EstaOcupado;

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsMensajeExito;

    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsMensajeExito;

    public string TituloFormulario => EsModoEdicion
        ? "Editar Proveedor"
        : "Registrar Nuevo Proveedor";

    public string TextoBotonGuardar => EstaOcupado
        ? "Guardando..."
        : (EsModoEdicion ? "Guardar Cambios" : "Guardar Proveedor");

    public bool TieneDocumentoEnFormulario =>
        !string.IsNullOrWhiteSpace(RutaDocumentoAdjuntoForm);

    public string NombreDocumentoFormTexto =>
        string.IsNullOrWhiteSpace(RutaDocumentoAdjuntoForm)
            ? "Ningún contrato o política adjunta (.pdf / .docx)"
            : Path.GetFileName(RutaDocumentoAdjuntoForm);

    partial void OnTextoBusquedaChanged(string value)
    {
        AplicarFiltroLocal();
    }

    [RelayCommand]
    public async Task CargarProveedoresAsync()
    {
        EstaOcupado = true;
        try
        {
            var lista = await _proveedorRepository.ObtenerTodosAsync();
            _todosLosProveedores = lista
                .OrderBy(p => p.NombreEmpresa)
                .ToList();

            TotalProveedoresCount = _todosLosProveedores.Count;
            AplicarFiltroLocal();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudieron cargar los proveedores: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AplicarFiltroLocal()
    {
        var filtro = TextoBusqueda?.Trim();

        IEnumerable<Proveedor> filtrados = _todosLosProveedores;
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            filtrados = _todosLosProveedores.Where(p =>
                p.NombreEmpresa.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(p.NombreComercial) &&
                 p.NombreComercial.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(p.Nit) &&
                 p.Nit.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(p.TelefonosContacto) &&
                 p.TelefonosContacto.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(p.ContactoWhatsapp) &&
                 p.ContactoWhatsapp.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(p.EmailInstitucional) &&
                 p.EmailInstitucional.Contains(filtro, StringComparison.OrdinalIgnoreCase)));
        }

        Proveedores.Clear();
        foreach (var item in filtrados)
        {
            Proveedores.Add(item);
        }

        OnPropertyChanged(nameof(SinResultados));
    }

    [RelayCommand]
    private void NuevoProveedor()
    {
        LimpiarMensaje();
        EsModoEdicion = false;
        _idProveedorEditando = null;

        NombreEmpresaForm = string.Empty;
        NombreComercialForm = string.Empty;
        NitForm = string.Empty;
        LicenciaFuncionamientoForm = string.Empty;
        CiContactoForm = string.Empty;
        TelefonosContactoForm = string.Empty;
        EmailInstitucionalForm = string.Empty;
        DireccionAlmacenForm = string.Empty;

        MonedaFacturacionSeleccionada = "BOB";
        FormasPagoAceptadasForm = string.Empty;
        TiemposEntregaForm = string.Empty;
        PoliticaFletesForm = string.Empty;
        PoliticaGarantiaYCambiosForm = string.Empty;

        RutaDocumentoAdjuntoForm = null;
        FormularioVisible = true;
    }

    [RelayCommand]
    private void EditarProveedor(Proveedor? proveedor)
    {
        var objetivo = proveedor ?? ProveedorSeleccionado;
        if (objetivo is null)
            return;

        LimpiarMensaje();
        ProveedorSeleccionado = objetivo;
        EsModoEdicion = true;
        _idProveedorEditando = objetivo.Id;

        NombreEmpresaForm = objetivo.NombreEmpresa;
        NombreComercialForm = objetivo.NombreComercial ?? string.Empty;
        NitForm = objetivo.Nit ?? string.Empty;
        LicenciaFuncionamientoForm = objetivo.LicenciaFuncionamiento ?? string.Empty;
        CiContactoForm = objetivo.CiContacto ?? string.Empty;
        TelefonosContactoForm = !string.IsNullOrWhiteSpace(objetivo.TelefonosContacto)
            ? objetivo.TelefonosContacto
            : (objetivo.ContactoWhatsapp ?? string.Empty);
        EmailInstitucionalForm = objetivo.EmailInstitucional ?? string.Empty;
        DireccionAlmacenForm = objetivo.DireccionAlmacen ?? string.Empty;

        MonedaFacturacionSeleccionada = !string.IsNullOrWhiteSpace(objetivo.MonedaFacturacion) &&
                                        MonedasDisponibles.Contains(objetivo.MonedaFacturacion)
            ? objetivo.MonedaFacturacion
            : "BOB";

        FormasPagoAceptadasForm = objetivo.FormasPagoAceptadas ?? string.Empty;
        TiemposEntregaForm = objetivo.TiemposEntrega ?? string.Empty;
        PoliticaFletesForm = objetivo.PoliticaFletes ?? string.Empty;
        PoliticaGarantiaYCambiosForm = objetivo.PoliticaGarantiaYCambios ?? string.Empty;

        RutaDocumentoAdjuntoForm = objetivo.RutaDocumentoAdjunto;
        FormularioVisible = true;
    }

    [RelayCommand]
    private void CerrarFormulario()
    {
        FormularioVisible = false;
    }

    /// <summary>
    /// Abre el selector de archivos (.pdf, .docx), copia el contrato/política a
    /// <c>%APPDATA%\FerreteriaERP\Documentos</c> y guarda la ruta en <see cref="RutaDocumentoAdjuntoForm"/>.
    /// </summary>
    [RelayCommand]
    private async Task AdjuntarDocumentoAsync()
    {
        LimpiarMensaje();

        var topLevel = ObtenerTopLevelActivo();
        if (topLevel?.StorageProvider is null)
        {
            MostrarErrorEstado("No se pudo abrir el explorador de archivos de la ventana principal.");
            return;
        }

        var filtroDocumentos = new FilePickerFileType("Contratos y Políticas (*.pdf, *.docx, *.doc)")
        {
            Patterns = new[] { "*.pdf", "*.docx", "*.doc" },
            MimeTypes = new[]
            {
                "application/pdf",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/msword"
            }
        };

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Adjuntar contrato o política del proveedor",
            AllowMultiple = false,
            FileTypeFilter = new[] { filtroDocumentos }
        });

        var archivoSeleccionado = archivos.FirstOrDefault();
        if (archivoSeleccionado is null)
            return;

        var rutaOrigen = archivoSeleccionado.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(rutaOrigen) || !File.Exists(rutaOrigen))
        {
            MostrarErrorEstado("No se pudo acceder al documento seleccionado.");
            return;
        }

        try
        {
            Directory.CreateDirectory(CarpetaDocumentosLocales);

            var nombreOriginal = Path.GetFileName(rutaOrigen);
            var nombreUnico = $"{Guid.NewGuid():N}_{nombreOriginal}";
            var rutaDestino = Path.Combine(CarpetaDocumentosLocales, nombreUnico);

            await Task.Run(() => File.Copy(rutaOrigen, rutaDestino, overwrite: true));

            RutaDocumentoAdjuntoForm = rutaDestino;
            MostrarExitoEstado($"Documento '{nombreOriginal}' adjuntado correctamente.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo copiar el documento adjunto: {ex.Message}");
        }
    }

    /// <summary>
    /// Abre el documento adjunto actual del formulario o de la tarjeta del proveedor con la aplicación predeterminada del sistema.
    /// </summary>
    [RelayCommand]
    private void AbrirDocumento(Proveedor? proveedor)
    {
        LimpiarMensaje();

        var ruta = proveedor is not null
            ? proveedor.RutaDocumentoAdjunto
            : RutaDocumentoAdjuntoForm;

        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
        {
            MostrarErrorEstado("El documento adjunto no se encuentra disponible en el disco local.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ruta,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo abrir el documento: {ex.Message}");
        }
    }

    [RelayCommand]
    private void QuitarDocumentoAdjunto()
    {
        RutaDocumentoAdjuntoForm = null;
    }

    [RelayCommand]
    private async Task GuardarProveedorAsync()
    {
        LimpiarMensaje();

        if (string.IsNullOrWhiteSpace(NombreEmpresaForm))
        {
            MostrarErrorEstado("La Razón Social / Nombre de Empresa del proveedor es obligatoria.");
            return;
        }

        EstaOcupado = true;
        try
        {
            var telefonoLimpio = NullSiVacio(TelefonosContactoForm);

            var entidad = new Proveedor
            {
                Id = _idProveedorEditando ?? 0,
                NombreEmpresa = NombreEmpresaForm.Trim(),
                NombreComercial = NullSiVacio(NombreComercialForm),
                Nit = NullSiVacio(NitForm),
                LicenciaFuncionamiento = NullSiVacio(LicenciaFuncionamientoForm),
                CiContacto = NullSiVacio(CiContactoForm),
                TelefonosContacto = telefonoLimpio,
                ContactoWhatsapp = telefonoLimpio,
                EmailInstitucional = NullSiVacio(EmailInstitucionalForm),
                DireccionAlmacen = NullSiVacio(DireccionAlmacenForm),
                MonedaFacturacion = string.IsNullOrWhiteSpace(MonedaFacturacionSeleccionada)
                    ? "BOB"
                    : MonedaFacturacionSeleccionada,
                FormasPagoAceptadas = NullSiVacio(FormasPagoAceptadasForm),
                TiemposEntrega = NullSiVacio(TiemposEntregaForm),
                PoliticaFletes = NullSiVacio(PoliticaFletesForm),
                PoliticaGarantiaYCambios = NullSiVacio(PoliticaGarantiaYCambiosForm),
                RutaDocumentoAdjunto = NullSiVacio(RutaDocumentoAdjuntoForm)
            };

            if (EsModoEdicion && _idProveedorEditando is not null)
            {
                await _proveedorRepository.ActualizarAsync(entidad);
                MostrarExitoEstado($"Proveedor '{entidad.NombreEmpresa}' actualizado correctamente.");
            }
            else
            {
                await _proveedorRepository.AgregarAsync(entidad);
                MostrarExitoEstado($"Proveedor '{entidad.NombreEmpresa}' registrado exitosamente.");
            }

            FormularioVisible = false;
            await CargarProveedoresAsync();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo guardar el proveedor: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private static string? NullSiVacio(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static TopLevel? ObtenerTopLevelActivo()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is not null)
        {
            return TopLevel.GetTopLevel(desktop.MainWindow);
        }

        return null;
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

