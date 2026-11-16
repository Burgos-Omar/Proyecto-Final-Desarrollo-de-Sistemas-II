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
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Services;
using FerreteriaERP.Infrastructure.Data;
using FerreteriaERP.UI.Desktop.Models;
using FerreteriaERP.UI.Desktop.Views;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class EmpleadosViewModel : ViewModelBase
{
    private static readonly string CarpetaFotosEmpleados = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FerreteriaERP",
        "Empleados");

    private readonly IDbContextFactory<FerreteriaDbContext> _dbContextFactory;
    private readonly ISesionService _sesionService;
    private readonly IPasswordHasherService _passwordHasher;

    private List<Empleado> _todosEmpleadosCargados = new();

    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    private bool _formularioVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloFormulario))]
    [NotifyPropertyChangedFor(nameof(TextoBotonGuardar))]
    [NotifyPropertyChangedFor(nameof(EsNuevoRegistro))]
    [NotifyPropertyChangedFor(nameof(PuedeGuardarEmpleado))]
    [NotifyCanExecuteChangedFor(nameof(GuardarEmpleadoCommand))]
    private bool _esModoEdicion;

    private int? _idEmpleadoEditando;

    [ObservableProperty]
    private Empleado? _empleadoSeleccionado;

    // Campos del formulario de Empleado
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardarEmpleado))]
    [NotifyCanExecuteChangedFor(nameof(GuardarEmpleadoCommand))]
    private string _nombreForm = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardarEmpleado))]
    [NotifyCanExecuteChangedFor(nameof(GuardarEmpleadoCommand))]
    private string _apellidosForm = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardarEmpleado))]
    [NotifyCanExecuteChangedFor(nameof(GuardarEmpleadoCommand))]
    private string _ciForm = string.Empty;

    [ObservableProperty]
    private string _telefonoForm = string.Empty;

    [ObservableProperty]
    private string _direccionForm = string.Empty;

    [ObservableProperty]
    private string _salarioTexto = "3000.00";

    [ObservableProperty]
    private string _puestoSeleccionado = "Vendedor";

    [ObservableProperty]
    private Sucursal? _sucursalSeleccionadaForm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneFotoSeleccionada))]
    [NotifyPropertyChangedFor(nameof(NombreArchivoFotoTexto))]
    private string? _imagenUrlForm;

    // Credenciales de acceso al sistema para el nuevo empleado (PASO 20)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardarEmpleado))]
    [NotifyCanExecuteChangedFor(nameof(GuardarEmpleadoCommand))]
    private string _nuevoUsuarioAcceso = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardarEmpleado))]
    [NotifyCanExecuteChangedFor(nameof(GuardarEmpleadoCommand))]
    private string _nuevaPasswordAcceso = string.Empty;

    // Gestor de Credenciales completo por Administrador (PASO 21 / PASO 22)
    [ObservableProperty]
    private string _usuarioAccesoActual = string.Empty;

    [ObservableProperty]
    private string _nuevaPasswordReseteo = string.Empty;

    // Estado y alertas
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ControlesHabilitados))]
    [NotifyPropertyChangedFor(nameof(TextoBotonGuardar))]
    [NotifyPropertyChangedFor(nameof(PuedeGuardarEmpleado))]
    [NotifyCanExecuteChangedFor(nameof(GuardarEmpleadoCommand))]
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
    private int _totalActivosCount;

    [ObservableProperty]
    private int _totalExEmpleadosCount;

    [ObservableProperty]
    private decimal _totalRecaudadoVendedores;

    public EmpleadosViewModel(
        IDbContextFactory<FerreteriaDbContext> dbContextFactory,
        ISesionService sesionService,
        IPasswordHasherService passwordHasher)
    {
        _dbContextFactory = dbContextFactory;
        _sesionService = sesionService;
        _passwordHasher = passwordHasher;

        _ = InicializarAsync();
    }

    /// <summary>
    /// Lista de personal con <see cref="Empleado.EstaActivo"/> == true.
    /// </summary>
    public ObservableCollection<Empleado> EmpleadosActivos { get; } = new();

    /// <summary>
    /// Lista de ex-trabajadores con <see cref="Empleado.EstaActivo"/> == false.
    /// </summary>
    public ObservableCollection<Empleado> ExEmpleados { get; } = new();

    /// <summary>
    /// Ranking (Tier List) de vendedores ordenado por cantidad de ventas y monto total recaudado en Bs.
    /// </summary>
    public ObservableCollection<VendedorRankingItem> RankingVendedores { get; } = new();

    public ObservableCollection<Sucursal> SucursalesDisponibles { get; } = new();

    public IReadOnlyList<string> PuestosDisponibles { get; } = new[]
    {
        "Vendedor",
        "Cajero",
        "Almacenero",
        "Chofer",
        "Encargado",
        "Administrativo"
    };

    /// <summary>
    /// Indica si el usuario logueado pertenece a la "Sucursal Central" (ve empleados de todas las sucursales).
    /// </summary>
    public bool EsSucursalCentral
    {
        get
        {
            var sucursal = _sesionService.UsuarioActual?.Sucursal;
            if (sucursal is null)
                return false;

            return sucursal.EsCentral ||
                   string.Equals(sucursal.Nombre?.Trim(), "Sucursal Central", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Indica si el usuario logueado tiene el rol de Administrador leyendo de <see cref="SesionActual"/>.
    /// </summary>
    public bool EsAdmin => SesionActual.EsAdmin;

    /// <summary>
    /// Alias de compatibilidad para la pestaña de Ranking de Ventas.
    /// </summary>
    public bool EsUsuarioAdmin => EsAdmin;

    public string SubtituloAlcanceSucursal => EsSucursalCentral
        ? "Vista Global (Sucursal Central): Supervisando personal de todas las sucursales."
        : $"Vista Local ({_sesionService.UsuarioActual?.Sucursal?.Nombre ?? "Sucursal Actual"}): Personal asignado a esta sucursal.";

    public string TotalRecaudadoVendedoresFormateado => $"Bs. {TotalRecaudadoVendedores:N2}";

    public bool SinEmpleadosActivos => EmpleadosActivos.Count == 0;

    public bool SinExEmpleados => ExEmpleados.Count == 0;

    public bool SinRankingVendedores => RankingVendedores.Count == 0;

    public bool ControlesHabilitados => !EstaOcupado;

    public bool EsNuevoRegistro => !EsModoEdicion;

    /// <summary>
    /// Habilita el botón Guardar solo cuando los campos obligatorios (incluyendo Usuario y Contraseña de mín. 6 caracteres en nuevo registro) están completos.
    /// </summary>
    public bool PuedeGuardarEmpleado =>
        !EstaOcupado &&
        !string.IsNullOrWhiteSpace(NombreForm) &&
        !string.IsNullOrWhiteSpace(ApellidosForm) &&
        !string.IsNullOrWhiteSpace(CiForm) &&
        (EsModoEdicion ||
         (!string.IsNullOrWhiteSpace(NuevoUsuarioAcceso) &&
          !string.IsNullOrWhiteSpace(NuevaPasswordAcceso) &&
          NuevaPasswordAcceso.Trim().Length >= 6));

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsMensajeExito;

    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsMensajeExito;

    public string TituloFormulario => EsModoEdicion
        ? "Editar Ficha de Empleado"
        : "Registrar Nuevo Empleado";

    public string TextoBotonGuardar => EstaOcupado
        ? "Guardando..."
        : (EsModoEdicion ? "Guardar Cambios" : "Registrar Empleado");

    public bool TieneFotoSeleccionada => !string.IsNullOrWhiteSpace(ImagenUrlForm);

    public string NombreArchivoFotoTexto => string.IsNullOrWhiteSpace(ImagenUrlForm)
        ? "Sin fotografía seleccionada (se mostrarán sus iniciales)"
        : Path.GetFileName(ImagenUrlForm);

    partial void OnTextoBusquedaChanged(string value)
    {
        AplicarFiltroListas(value);
    }

    [RelayCommand]
    public async Task InicializarAsync()
    {
        EstaOcupado = true;
        LimpiarMensaje();

        try
        {
            await CargarEmpleadosYSucursalesCoreAsync();
            await CargarRankingVendedoresAsync();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo cargar el módulo de empleados: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task CargarEmpleadosYSucursalesCoreAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var sucursales = await db.Sucursales
            .AsNoTracking()
            .OrderByDescending(s => s.EsCentral)
            .ThenBy(s => s.Nombre)
            .ToListAsync();

        SucursalesDisponibles.Clear();
        foreach (var suc in sucursales)
        {
            SucursalesDisponibles.Add(suc);
        }

        var query = db.Empleados
            .AsNoTracking()
            .Include(e => e.Sucursal)
            .AsQueryable();

        // Lógica de Sucursales: Central carga todos; otra sucursal carga solo los de su IdSucursal
        if (!EsSucursalCentral && _sesionService.EstaAutenticado)
        {
            var idSucursalActual = _sesionService.IdSucursalActual;
            query = query.Where(e => e.IdSucursal == idSucursalActual);
        }

        _todosEmpleadosCargados = await query
            .OrderBy(e => e.Nombre)
            .ThenBy(e => e.Apellidos)
            .ToListAsync();

        AplicarFiltroListas(TextoBusqueda);
    }

    private void AplicarFiltroListas(string? filtroTexto)
    {
        var termino = filtroTexto?.Trim();
        IEnumerable<Empleado> fuente = _todosEmpleadosCargados;

        if (!string.IsNullOrWhiteSpace(termino))
        {
            fuente = _todosEmpleadosCargados.Where(e =>
                e.NombreCompleto.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                e.Ci.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                e.Puesto.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                (e.Sucursal?.Nombre?.Contains(termino, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        EmpleadosActivos.Clear();
        ExEmpleados.Clear();

        foreach (var emp in fuente)
        {
            if (emp.EstaActivo)
            {
                EmpleadosActivos.Add(emp);
            }
            else
            {
                ExEmpleados.Add(emp);
            }
        }

        TotalActivosCount = _todosEmpleadosCargados.Count(e => e.EstaActivo);
        TotalExEmpleadosCount = _todosEmpleadosCargados.Count(e => !e.EstaActivo);

        OnPropertyChanged(nameof(SinEmpleadosActivos));
        OnPropertyChanged(nameof(SinExEmpleados));
    }

    /// <summary>
    /// Cruza la tabla <see cref="Empleado"/> (donde <c>Puesto == "Vendedor"</c>) con la tabla <see cref="Venta"/>,
    /// devolviendo el ranking (Tier List) ordenado por cantidad de ventas concretadas y total recaudado en Bs.
    /// </summary>
    [RelayCommand]
    public async Task CargarRankingVendedoresAsync()
    {
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var vendedoresQuery = db.Empleados
                .AsNoTracking()
                .Include(e => e.Sucursal)
                .Where(e => e.Puesto == "Vendedor");

            if (!EsSucursalCentral && _sesionService.EstaAutenticado)
            {
                var idSucursalActual = _sesionService.IdSucursalActual;
                vendedoresQuery = vendedoresQuery.Where(e => e.IdSucursal == idSucursalActual);
            }

            var vendedores = await vendedoresQuery.ToListAsync();

            var estadisticasVentas = await db.Ventas
                .AsNoTracking()
                .Where(v => v.IdEmpleado != null)
                .GroupBy(v => v.IdEmpleado!.Value)
                .Select(g => new
                {
                    IdEmpleado = g.Key,
                    CantidadVentas = g.Count(),
                    MontoTotal = g.Sum(v => v.Total)
                })
                .ToListAsync();

            var mapaVentas = estadisticasVentas.ToDictionary(x => x.IdEmpleado);

            var ordenados = vendedores
                .Select(v =>
                {
                    mapaVentas.TryGetValue(v.Id, out var stats);
                    return new
                    {
                        Empleado = v,
                        CantidadVentas = stats?.CantidadVentas ?? 0,
                        MontoTotal = stats?.MontoTotal ?? 0m
                    };
                })
                .OrderByDescending(x => x.CantidadVentas)
                .ThenByDescending(x => x.MontoTotal)
                .ThenBy(x => x.Empleado.NombreCompleto)
                .ToList();

            RankingVendedores.Clear();
            var posicion = 1;
            var sumaGlobal = 0m;

            foreach (var item in ordenados)
            {
                sumaGlobal += item.MontoTotal;
                RankingVendedores.Add(new VendedorRankingItem
                {
                    Posicion = posicion++,
                    Empleado = item.Empleado,
                    CantidadVentas = item.CantidadVentas,
                    MontoTotalVendido = item.MontoTotal
                });
            }

            TotalRecaudadoVendedores = sumaGlobal;
            OnPropertyChanged(nameof(TotalRecaudadoVendedoresFormateado));
            OnPropertyChanged(nameof(SinRankingVendedores));
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo calcular el ranking de ventas: {ex.Message}");
        }
    }

    /// <summary>
    /// Abre <see cref="IStorageProvider"/>, copia la fotografía seleccionada a
    /// <c>%APPDATA%\FerreteriaERP\Empleados</c> y asigna la ruta local a <see cref="ImagenUrlForm"/>.
    /// </summary>
    [RelayCommand]
    private async Task SubirFotoAsync()
    {
        LimpiarMensaje();

        try
        {
            var topLevel = ObtenerTopLevelActivo();
            if (topLevel?.StorageProvider is null)
            {
                MostrarErrorEstado("No se pudo acceder al explorador de archivos del sistema.");
                return;
            }

            var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar Fotografía del Empleado",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imágenes (*.png, *.jpg, *.jpeg)")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg" }
                    }
                }
            });

            if (archivos is null || archivos.Count == 0)
                return;

            var archivoOrigen = archivos[0];
            var rutaOrigen = archivoOrigen.TryGetLocalPath();
            var extension = Path.GetExtension(archivoOrigen.Name);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            Directory.CreateDirectory(CarpetaFotosEmpleados);
            var nombreDestino = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var rutaDestino = Path.Combine(CarpetaFotosEmpleados, nombreDestino);

            if (!string.IsNullOrWhiteSpace(rutaOrigen) && File.Exists(rutaOrigen))
            {
                File.Copy(rutaOrigen, rutaDestino, overwrite: true);
            }
            else
            {
                await using var streamOrigen = await archivoOrigen.OpenReadAsync();
                await using var streamDestino = File.Create(rutaDestino);
                await streamOrigen.CopyToAsync(streamDestino);
            }

            ImagenUrlForm = rutaDestino;
            MostrarExitoEstado("Fotografía cargada y almacenada localmente.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo copiar la fotografía del empleado: {ex.Message}");
        }
    }

    [RelayCommand]
    private void QuitarFotoFormulario()
    {
        ImagenUrlForm = null;
    }

    /// <summary>
    /// Cambia <see cref="Empleado.EstaActivo"/> a <c>false</c> (Inhabilitado) o <c>true</c> (Activo)
    /// en la base de datos y mueve al empleado entre <see cref="EmpleadosActivos"/> y <see cref="ExEmpleados"/>.
    /// </summary>
    [RelayCommand]
    private async Task CambiarEstadoAsync(Empleado? empleado)
    {
        if (empleado is null)
            return;

        EstaOcupado = true;
        LimpiarMensaje();

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var entidad = await db.Empleados.FirstOrDefaultAsync(e => e.Id == empleado.Id);
            if (entidad is null)
            {
                MostrarErrorEstado("No se encontró el registro del empleado en la base de datos.");
                return;
            }

            entidad.EstaActivo = !entidad.EstaActivo;
            await db.SaveChangesAsync();

            empleado.EstaActivo = entidad.EstaActivo;
            AplicarFiltroListas(TextoBusqueda);
            await CargarRankingVendedoresAsync();

            var accion = entidad.EstaActivo ? "reincorporado a Personal Activo" : "movido a Ex-Trabajadores";
            MostrarExitoEstado($"El empleado '{empleado.NombreCompleto}' fue {accion}.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al cambiar el estado del empleado: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    [RelayCommand]
    private void AbrirFormularioNuevo()
    {
        LimpiarMensaje();
        _idEmpleadoEditando = null;
        EmpleadoSeleccionado = null;
        EsModoEdicion = false;

        NombreForm = string.Empty;
        ApellidosForm = string.Empty;
        CiForm = string.Empty;
        TelefonoForm = string.Empty;
        DireccionForm = string.Empty;
        SalarioTexto = "3000.00";
        PuestoSeleccionado = "Vendedor";
        ImagenUrlForm = null;
        NuevoUsuarioAcceso = string.Empty;
        NuevaPasswordAcceso = string.Empty;
        UsuarioAccesoActual = string.Empty;
        NuevaPasswordReseteo = string.Empty;

        var idSucursalActual = _sesionService.IdSucursalActual;
        SucursalSeleccionadaForm = SucursalesDisponibles.FirstOrDefault(s => s.Id == idSucursalActual)
                                   ?? SucursalesDisponibles.FirstOrDefault();

        FormularioVisible = true;
    }

    [RelayCommand]
    private void AbrirFormularioEdicion(Empleado? empleado)
    {
        if (empleado is null)
            return;

        LimpiarMensaje();
        _idEmpleadoEditando = empleado.Id;
        EmpleadoSeleccionado = null;
        EmpleadoSeleccionado = empleado;
        EsModoEdicion = true;

        NombreForm = empleado.Nombre;
        ApellidosForm = empleado.Apellidos;
        CiForm = empleado.Ci;
        TelefonoForm = empleado.Telefono ?? string.Empty;
        DireccionForm = empleado.Direccion ?? string.Empty;
        SalarioTexto = empleado.Salario.ToString("0.00", CultureInfo.InvariantCulture);
        PuestoSeleccionado = PuestosDisponibles.Contains(empleado.Puesto)
            ? empleado.Puesto
            : "Vendedor";
        ImagenUrlForm = empleado.ImagenUrl;
        NuevoUsuarioAcceso = string.Empty;
        NuevaPasswordAcceso = string.Empty;
        NuevaPasswordReseteo = string.Empty;
        SucursalSeleccionadaForm = SucursalesDisponibles.FirstOrDefault(s => s.Id == empleado.IdSucursal)
                                   ?? SucursalesDisponibles.FirstOrDefault();

        FormularioVisible = true;
    }

    partial void OnEmpleadoSeleccionadoChanged(Empleado? value)
    {
        if (value is null)
        {
            UsuarioAccesoActual = string.Empty;
            return;
        }

        _ = CargarUsuarioAccesoEmpleadoAsync(value);
    }

    private async Task CargarUsuarioAccesoEmpleadoAsync(Empleado empleado)
    {
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var nombreCompletoLower = empleado.NombreCompleto.Trim().ToLower();
            var ciLower = empleado.Ci.Trim().ToLower();

            var usuarioVinculado = await db.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.NombreCompleto.ToLower() == nombreCompletoLower ||
                    u.Ci.ToLower() == ciLower);

            if (EmpleadoSeleccionado?.Id == empleado.Id)
            {
                UsuarioAccesoActual = usuarioVinculado?.Ci ?? empleado.Ci;
            }
        }
        catch
        {
            if (EmpleadoSeleccionado?.Id == empleado.Id)
            {
                UsuarioAccesoActual = empleado.Ci;
            }
        }
    }

    [RelayCommand]
    private void CerrarFormulario()
    {
        FormularioVisible = false;
        _idEmpleadoEditando = null;
        EmpleadoSeleccionado = null;
        UsuarioAccesoActual = string.Empty;
        NuevaPasswordReseteo = string.Empty;
    }

    public IAsyncRelayCommand RestablecerPasswordCommand => ActualizarCredencialesCommand;

    /// <summary>
    /// PASO 22: Actualiza el nombre de usuario y opcionalmente la contraseña del Usuario vinculado al empleado seleccionado (exclusivo para Administradores).
    /// </summary>
    [RelayCommand]
    private async Task ActualizarCredencialesAsync()
    {
        LimpiarMensaje();

        if (!SesionActual.EsAdmin)
            return;

        var empleado = EmpleadoSeleccionado
                       ?? (_idEmpleadoEditando.HasValue
                           ? _todosEmpleadosCargados.FirstOrDefault(e => e.Id == _idEmpleadoEditando.Value)
                           : null);

        var nuevoUsuario = UsuarioAccesoActual?.Trim() ?? string.Empty;
        var nuevaPassword = NuevaPasswordReseteo?.Trim() ?? string.Empty;

        if (empleado is null)
        {
            MostrarErrorEstado("Seleccione un empleado antes de actualizar sus credenciales.");
            return;
        }

        if (string.IsNullOrWhiteSpace(nuevoUsuario))
        {
            MostrarErrorEstado("El nombre de usuario del sistema no puede quedar vacío.");
            return;
        }

        EstaOcupado = true;
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var nombreCompletoLower = empleado.NombreCompleto.Trim().ToLower();
            var ciLower = empleado.Ci.Trim().ToLower();
            var nuevoUsuarioLower = nuevoUsuario.ToLower();

            var usuarioVinculado = await db.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.NombreCompleto.ToLower() == nombreCompletoLower ||
                    u.Ci.ToLower() == ciLower);

            var duplicadoEnOtro = await db.Usuarios.AnyAsync(u =>
                u.Ci.ToLower() == nuevoUsuarioLower &&
                (usuarioVinculado == null || u.Id != usuarioVinculado.Id));

            if (duplicadoEnOtro)
            {
                MostrarErrorEstado($"El nombre de usuario '{nuevoUsuario}' ya está asignado a otra cuenta.");
                return;
            }

            if (usuarioVinculado is not null)
            {
                usuarioVinculado.Ci = nuevoUsuario;

                if (!string.IsNullOrWhiteSpace(nuevaPassword))
                {
                    usuarioVinculado.PasswordHash = await Task.Run(() => _passwordHasher.HashPassword(nuevaPassword));
                }
            }
            else
            {
                var puestoRol = string.IsNullOrWhiteSpace(empleado.Puesto) ? "Vendedor" : empleado.Puesto.Trim();
                var rolAsignado = await db.Roles.FirstOrDefaultAsync(r => r.Nombre.ToLower() == puestoRol.ToLower())
                                  ?? new Rol { Nombre = puestoRol };

                var passwordInicial = !string.IsNullOrWhiteSpace(nuevaPassword) ? nuevaPassword : empleado.Ci.Trim();
                var hashInicial = await Task.Run(() => _passwordHasher.HashPassword(passwordInicial));

                usuarioVinculado = new Usuario
                {
                    Ci = nuevoUsuario,
                    NombreCompleto = empleado.NombreCompleto,
                    PasswordHash = hashInicial,
                    Celular = empleado.Telefono,
                    Direccion = empleado.Direccion,
                    Salario = empleado.Salario,
                    FechaInicio = DateTime.UtcNow,
                    FotoPath = empleado.ImagenUrl,
                    Estado = EstadosUsuario.Activo,
                    Rol = rolAsignado,
                    IdSucursal = empleado.IdSucursal
                };

                db.Usuarios.Add(usuarioVinculado);
            }

            await db.SaveChangesAsync();

            NuevaPasswordReseteo = string.Empty;
            var textoExito = $"Credenciales de {empleado.NombreCompleto} actualizadas correctamente";
            MostrarExitoEstado(textoExito);

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
                    "Seguridad y Accesos",
                    textoExito,
                    NotificationType.Success,
                    TimeSpan.FromSeconds(4)));
            });
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudieron actualizar las credenciales: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    public IAsyncRelayCommand GuardarNuevoEmpleadoCommand => GuardarEmpleadoCommand;

    [RelayCommand(CanExecute = nameof(PuedeGuardarEmpleado))]
    private async Task GuardarEmpleadoAsync()
    {
        LimpiarMensaje();

        var nombre = NombreForm?.Trim() ?? string.Empty;
        var apellidos = ApellidosForm?.Trim() ?? string.Empty;
        var ci = CiForm?.Trim() ?? string.Empty;
        var usuarioAcceso = NuevoUsuarioAcceso?.Trim() ?? string.Empty;
        var passwordAcceso = NuevaPasswordAcceso?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellidos))
        {
            MostrarErrorEstado("El Nombre y los Apellidos del empleado son obligatorios.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ci))
        {
            MostrarErrorEstado("El número de Carnet de Identidad (CI) es obligatorio.");
            return;
        }

        if (!EsModoEdicion)
        {
            if (string.IsNullOrWhiteSpace(usuarioAcceso))
            {
                MostrarErrorEstado("El nombre de usuario para acceso al sistema es obligatorio.");
                return;
            }

            if (string.IsNullOrWhiteSpace(passwordAcceso) || passwordAcceso.Length < 6)
            {
                MostrarErrorEstado("La contraseña de acceso debe tener al menos 6 caracteres.");
                return;
            }
        }

        var salarioNorm = (SalarioTexto ?? "0").Trim().Replace(',', '.');
        if (!decimal.TryParse(salarioNorm, NumberStyles.Number, CultureInfo.InvariantCulture, out var salario) || salario < 0m)
        {
            MostrarErrorEstado("Ingrese un salario mensual válido en Bolivianos (ej. 3200.00).");
            return;
        }

        var idSucursalDestino = SucursalSeleccionadaForm?.Id
                                ?? (_sesionService.IdSucursalActual > 0 ? _sesionService.IdSucursalActual : 1);

        var puestoFinal = string.IsNullOrWhiteSpace(PuestoSeleccionado) ? "Vendedor" : PuestoSeleccionado.Trim();

        EstaOcupado = true;
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var ciDuplicado = await db.Empleados.AnyAsync(e =>
                e.Ci.ToLower() == ci.ToLower() &&
                (!_idEmpleadoEditando.HasValue || e.Id != _idEmpleadoEditando.Value));

            if (ciDuplicado)
            {
                MostrarErrorEstado($"Ya existe otro empleado registrado con el CI '{ci}'.");
                return;
            }

            if (EsModoEdicion && _idEmpleadoEditando.HasValue)
            {
                var existente = await db.Empleados.FirstOrDefaultAsync(e => e.Id == _idEmpleadoEditando.Value);
                if (existente is null)
                {
                    MostrarErrorEstado("No se encontró el empleado que intenta editar.");
                    return;
                }

                existente.Nombre = nombre;
                existente.Apellidos = apellidos;
                existente.Ci = ci;
                existente.Telefono = string.IsNullOrWhiteSpace(TelefonoForm) ? null : TelefonoForm.Trim();
                existente.Direccion = string.IsNullOrWhiteSpace(DireccionForm) ? null : DireccionForm.Trim();
                existente.Salario = salario;
                existente.Puesto = puestoFinal;
                existente.ImagenUrl = ImagenUrlForm;
                existente.IdSucursal = idSucursalDestino;

                await db.SaveChangesAsync();
                MostrarExitoEstado($"Datos de '{existente.NombreCompleto}' actualizados correctamente.");
            }
            else
            {
                var usuarioDuplicado = await db.Usuarios.AnyAsync(u =>
                    u.Ci.ToLower() == usuarioAcceso.ToLower());

                if (usuarioDuplicado)
                {
                    MostrarErrorEstado($"El usuario de acceso '{usuarioAcceso}' ya está registrado en el sistema. Elija otro.");
                    return;
                }

                await using var transaccion = await db.Database.BeginTransactionAsync();

                var nuevo = new Empleado
                {
                    Nombre = nombre,
                    Apellidos = apellidos,
                    Ci = ci,
                    Telefono = string.IsNullOrWhiteSpace(TelefonoForm) ? null : TelefonoForm.Trim(),
                    Direccion = string.IsNullOrWhiteSpace(DireccionForm) ? null : DireccionForm.Trim(),
                    Salario = salario,
                    Puesto = puestoFinal,
                    ImagenUrl = ImagenUrlForm,
                    IdSucursal = idSucursalDestino,
                    EstaActivo = true
                };

                db.Empleados.Add(nuevo);

                // Buscar o crear el Rol equivalente al Puesto asignado
                var rolAsignado = await db.Roles.FirstOrDefaultAsync(r =>
                                      r.Nombre.ToLower() == puestoFinal.ToLower())
                                  ?? new Rol
                                  {
                                      Nombre = puestoFinal
                                  };

                var passwordHash = await Task.Run(() => _passwordHasher.HashPassword(passwordAcceso));

                var nuevoUsuario = new Usuario
                {
                    Ci = usuarioAcceso,
                    NombreCompleto = nuevo.NombreCompleto,
                    PasswordHash = passwordHash,
                    Celular = nuevo.Telefono,
                    Direccion = nuevo.Direccion,
                    Salario = salario,
                    FechaInicio = DateTime.UtcNow,
                    FotoPath = ImagenUrlForm,
                    Estado = EstadosUsuario.Activo,
                    Rol = rolAsignado,
                    IdSucursal = idSucursalDestino
                };

                db.Usuarios.Add(nuevoUsuario);

                await db.SaveChangesAsync();
                await transaccion.CommitAsync();

                MostrarExitoEstado(
                    $"Empleado '{nuevo.NombreCompleto}' y usuario '{usuarioAcceso}' ({puestoFinal}) registrados exitosamente.");
            }

            FormularioVisible = false;
            _idEmpleadoEditando = null;

            await CargarEmpleadosYSucursalesCoreAsync();
            await CargarRankingVendedoresAsync();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al guardar el empleado: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
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

