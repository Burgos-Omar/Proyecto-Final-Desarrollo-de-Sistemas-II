using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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
using FerreteriaERP.Core.Exceptions;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using FerreteriaERP.Infrastructure.Services;
using FerreteriaERP.UI.Desktop.Views;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class PosViewModel : ViewModelBase
{
    private readonly IProductoRepository _productoRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly ISesionService _sesionService;
    private readonly IDbContextFactory<FerreteriaDbContext> _dbContextFactory;
    private readonly TicketPdfService _ticketPdfService;

    private List<Producto> _productosConStockSucursal = new();
    private bool _sincronizandoMontoTexto;

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
    private decimal _montoRecibido;

    [ObservableProperty]
    private string _montoRecibidoTexto = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CambioFormateado))]
    private decimal _cambioDado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsPagoEfectivo))]
    [NotifyPropertyChangedFor(nameof(NoEsPagoEfectivo))]
    [NotifyPropertyChangedFor(nameof(EsPagoQr))]
    [NotifyPropertyChangedFor(nameof(NoEsPagoQr))]
    [NotifyPropertyChangedFor(nameof(IsPagoCredito))]
    [NotifyPropertyChangedFor(nameof(EsPagoCredito))]
    [NotifyPropertyChangedFor(nameof(NoEsPagoCredito))]
    private string _metodoPagoSeleccionado = MetodosPago.Efectivo;

    // Fecha límite configurable para ventas a crédito (PASO 18)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FechaLimiteCreditoTexto))]
    private DateTime? _fechaLimiteCredito = DateTime.Today.AddDays(30);

    // Gestión de Cliente para Ventas a Crédito (PASO 8)
    [ObservableProperty]
    private string _ciNitBusqueda = string.Empty;

    [ObservableProperty]
    private string _nombreClienteNuevo = string.Empty;

    [ObservableProperty]
    private string _celularClienteNuevo = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneClienteSeleccionado))]
    [NotifyPropertyChangedFor(nameof(TextoClienteConfirmado))]
    [NotifyPropertyChangedFor(nameof(DetalleClienteConfirmado))]
    private Cliente? _clienteSeleccionado;

    // Sistema de Recompensas en POS: Vendedor que concretó la venta (PASO 14)
    [ObservableProperty]
    private Empleado? _vendedorSeleccionado;

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
    [NotifyPropertyChangedFor(nameof(TextoBotonCompletar))]
    [NotifyCanExecuteChangedFor(nameof(CompletarVentaCommand))]
    private bool _estaProcesando;

    public PosViewModel(
        IProductoRepository productoRepository,
        IVentaRepository ventaRepository,
        IClienteRepository clienteRepository,
        ISesionService sesionService,
        IDbContextFactory<FerreteriaDbContext> dbContextFactory,
        TicketPdfService ticketPdfService)
    {
        _productoRepository = productoRepository;
        _ventaRepository = ventaRepository;
        _clienteRepository = clienteRepository;
        _sesionService = sesionService;
        _dbContextFactory = dbContextFactory;
        _ticketPdfService = ticketPdfService;

        Carrito.CollectionChanged += (_, _) => RecalcularTotales();

        _ = CargarCatalogoAsync();
    }

    /// <summary>
    /// Lista de vendedores activos de la sucursal actual (<c>Puesto == "Vendedor"</c> y <c>EstaActivo == true</c>).
    /// </summary>
    public ObservableCollection<Empleado> VendedoresDisponibles { get; } = new();

    /// <summary>
    /// Catálogo visual de productos con stock disponible en la sucursal actual, filtrado dinámicamente.
    /// </summary>
    public ObservableCollection<Producto> CatalogoVisible { get; } = new();

    public ObservableCollection<DetalleVenta> Carrito { get; } = new();

    public bool CatalogoVacio => CatalogoVisible.Count == 0;

    public bool TieneTextoBusqueda => !string.IsNullOrWhiteSpace(TextoBusqueda);

    public bool CarritoVacio => Carrito.Count == 0;

    public bool TieneProductosEnCarrito => Carrito.Count > 0;

    public string TotalFormateado => $"Bs {Total:N2}";

    public string CambioFormateado => $"Bs {CambioDado:N2}";

    public string ResumenArticulos => TotalArticulos == 1
        ? "1 unidad en carrito"
        : $"{TotalArticulos} unidades en carrito";

    public bool EsPagoEfectivo => MetodoPagoSeleccionado == MetodosPago.Efectivo;
    public bool NoEsPagoEfectivo => !EsPagoEfectivo;

    public bool EsPagoQr => MetodoPagoSeleccionado == MetodosPago.QR;
    public bool NoEsPagoQr => !EsPagoQr;

    public bool IsPagoCredito => MetodoPagoSeleccionado == MetodosPago.Credito;
    public bool EsPagoCredito => IsPagoCredito;
    public bool NoEsPagoCredito => !IsPagoCredito;

    public bool TieneClienteSeleccionado => ClienteSeleccionado is not null;

    public string TextoClienteConfirmado => ClienteSeleccionado is null
        ? string.Empty
        : $"✓ Cliente asignado: {ClienteSeleccionado.NombreCompleto}";

    public string DetalleClienteConfirmado => ClienteSeleccionado is null
        ? string.Empty
        : $"CI/NIT: {ClienteSeleccionado.Ci ?? "S/N"}  •  WhatsApp: {(string.IsNullOrWhiteSpace(ClienteSeleccionado.CelularWhatsapp) ? "No registrado" : ClienteSeleccionado.CelularWhatsapp)}";

    /// <summary>
    /// Texto de presentación de la fecha límite de crédito seleccionada en el CalendarDatePicker.
    /// </summary>
    public string FechaLimiteCreditoTexto =>
        FechaLimiteCredito.HasValue
            ? $"Vence el {FechaLimiteCredito.Value:dd/MM/yyyy}"
            : "Sin fecha seleccionada (30 días por defecto)";

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsMensajeExito;
    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsMensajeExito;

    public bool ControlesHabilitados => !EstaProcesando;

    public string TextoBotonCompletar => EstaProcesando
        ? "PROCESANDO VENTA..."
        : "COMPLETAR VENTA";

    public string NombreSucursalActiva =>
        _sesionService.UsuarioActual?.Sucursal?.Nombre ?? "Sucursal Activa";

    public string NombreCajeroActivo =>
        _sesionService.UsuarioActual?.NombreCompleto ?? "Cajero";

    public event Action? SolicitarFocoEscaneador;

    partial void OnTextoBusquedaChanged(string value)
    {
        FiltrarCatalogoVisible(value);
    }

    partial void OnCiNitBusquedaChanged(string value)
    {
        if (ClienteSeleccionado is not null &&
            !string.Equals(ClienteSeleccionado.Ci, value?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            ClienteSeleccionado = null;
        }
    }

    partial void OnMontoRecibidoChanged(decimal value)
    {
        if (!_sincronizandoMontoTexto)
        {
            MontoRecibidoTexto = value <= 0m
                ? string.Empty
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        RecalcularCambio();
    }

    partial void OnMontoRecibidoTextoChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _sincronizandoMontoTexto = true;
            MontoRecibido = 0m;
            _sincronizandoMontoTexto = false;
            return;
        }

        var normalizado = value.Trim().Replace(',', '.');
        if (decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto) && monto >= 0m)
        {
            _sincronizandoMontoTexto = true;
            MontoRecibido = monto;
            _sincronizandoMontoTexto = false;
        }
    }

    partial void OnMetodoPagoSeleccionadoChanged(string value)
    {
        LimpiarMensaje();
        RecalcularCambio();
    }

    /// <summary>
    /// Carga todos los productos con stock positivo en la sucursal activa, ordenados alfabéticamente.
    /// </summary>
    [RelayCommand]
    public async Task CargarCatalogoAsync()
    {
        if (!_sesionService.EstaAutenticado)
            return;

        try
        {
            var idSucursal = _sesionService.IdSucursalActual;
            var todos = await _productoRepository.ObtenerCatalogoConStockSucursalAsync(idSucursal);

            _productosConStockSucursal = todos
                .Where(p => p.Stocks.Any(s => s.IdSucursal == idSucursal && s.CantidadActual > 0))
                .OrderBy(p => p.Nombre)
                .ToList();

            FiltrarCatalogoVisible(TextoBusqueda);
            await CargarVendedoresDisponiblesAsync(idSucursal);
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo cargar el catálogo visual: {ex.Message}");
        }
    }

    private async Task CargarVendedoresDisponiblesAsync(int idSucursal)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var vendedores = await db.Empleados
            .AsNoTracking()
            .Where(e => e.IdSucursal == idSucursal && e.EstaActivo && e.Puesto == "Vendedor")
            .OrderBy(e => e.Nombre)
            .ThenBy(e => e.Apellidos)
            .ToListAsync();

        var idSeleccionadoPrevio = VendedorSeleccionado?.Id;

        VendedoresDisponibles.Clear();
        foreach (var v in vendedores)
        {
            VendedoresDisponibles.Add(v);
        }

        VendedorSeleccionado = VendedoresDisponibles.FirstOrDefault(v => v.Id == idSeleccionadoPrevio)
                               ?? VendedoresDisponibles.FirstOrDefault();
    }

    private void FiltrarCatalogoVisible(string? texto)
    {
        var filtro = texto?.Trim();

        IEnumerable<Producto> filtrados = _productosConStockSucursal;
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            filtrados = _productosConStockSucursal.Where(p =>
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
        SolicitarFocoEscaneador?.Invoke();
    }

    /// <summary>
    /// Agrega directamente un producto al carrito al hacer clic en su tarjeta del catálogo visual.
    /// </summary>
    [RelayCommand]
    private async Task AgregarDesdeTarjetaAsync(Producto? producto)
    {
        if (producto is null)
            return;

        LimpiarMensaje();

        try
        {
            await AgregarOIncrementarEnCarritoAsync(producto);
            SolicitarFocoEscaneador?.Invoke();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo agregar el producto: {ex.Message}");
        }
    }

    /// <summary>
    /// Se ejecuta al presionar "Enter" en el buscador (lector USB de código de barras).
    /// Busca una coincidencia exacta por <see cref="Producto.CodigoBarras"/>; si la encuentra,
    /// agrega el producto al carrito y limpia el buscador; si no, no hace nada.
    /// </summary>
    [RelayCommand]
    private async Task ProcesarEscaneoAsync()
    {
        var codigo = TextoBusqueda?.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
            return;

        try
        {
            // 1. Buscar coincidencia exacta en la caché local o en la BD por CodigoBarras
            var producto = _productosConStockSucursal.FirstOrDefault(p =>
                               !string.IsNullOrWhiteSpace(p.CodigoBarras) &&
                               string.Equals(p.CodigoBarras.Trim(), codigo, StringComparison.OrdinalIgnoreCase))
                           ?? await _productoRepository.ObtenerPorCodigoBarrasAsync(codigo);

            if (producto is null)
            {
                // Si no hay coincidencia exacta de código de barras, no hace nada (mantiene el filtro por nombre)
                return;
            }

            LimpiarMensaje();
            await AgregarOIncrementarEnCarritoAsync(producto);
            TextoBusqueda = string.Empty;
            SolicitarFocoEscaneador?.Invoke();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al procesar el escaneo: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca un cliente en el repositorio por CI/NIT. Si existe, lo asigna a <see cref="ClienteSeleccionado"/>.
    /// Si no existe, lo crea al vuelo con el Nombre y Celular ingresados, lo guarda en la BD y lo selecciona.
    /// </summary>
    [RelayCommand]
    private async Task BuscarOCrearClienteAsync()
    {
        LimpiarMensaje();

        var ciNit = CiNitBusqueda?.Trim();
        if (string.IsNullOrWhiteSpace(ciNit))
        {
            MostrarErrorEstado("Ingrese el CI o NIT del cliente para buscarlo o registrarlo.");
            return;
        }

        try
        {
            var existente = await _clienteRepository.ObtenerPorCiAsync(ciNit);
            var celularIngresado = string.IsNullOrWhiteSpace(CelularClienteNuevo)
                ? null
                : CelularClienteNuevo.Trim();

            if (existente is not null)
            {
                if (!string.IsNullOrWhiteSpace(celularIngresado) &&
                    !string.Equals(existente.CelularWhatsapp, celularIngresado, StringComparison.Ordinal))
                {
                    existente.CelularWhatsapp = celularIngresado;
                    await _clienteRepository.ActualizarAsync(existente);
                }

                NombreClienteNuevo = existente.NombreCompleto;
                CelularClienteNuevo = existente.CelularWhatsapp ?? string.Empty;
                ClienteSeleccionado = existente;

                MostrarExitoEstado(
                    $"Cliente encontrado y confirmado: {existente.NombreCompleto} (CI/NIT: {existente.Ci}).");
                return;
            }

            var nombreIngresado = NombreClienteNuevo?.Trim();
            if (string.IsNullOrWhiteSpace(nombreIngresado))
            {
                ClienteSeleccionado = null;
                MostrarErrorEstado(
                    $"El CI/NIT '{ciNit}' no está registrado. Ingrese el Nombre Completo y Celular/WhatsApp y presione 'Confirmar Cliente' para crearlo.");
                return;
            }

            var nuevoCliente = await _clienteRepository.AgregarAsync(new Cliente
            {
                Ci = ciNit,
                NombreCompleto = nombreIngresado,
                CelularWhatsapp = celularIngresado,
                CantidadCompras = 0
            });

            ClienteSeleccionado = nuevoCliente;
            MostrarExitoEstado(
                $"Cliente nuevo registrado y confirmado: {nuevoCliente.NombreCompleto} (CI/NIT: {nuevoCliente.Ci}).");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo confirmar el cliente: {ex.Message}");
        }
    }

    private async Task AgregarOIncrementarEnCarritoAsync(Producto producto)
    {
        var idSucursal = _sesionService.IdSucursalActual;
        var stockDisponible = await _productoRepository.ObtenerStockAsync(producto.Id, idSucursal);

        var indiceExistente = -1;
        for (var i = 0; i < Carrito.Count; i++)
        {
            if (Carrito[i].IdProducto == producto.Id)
            {
                indiceExistente = i;
                break;
            }
        }

        var cantidadSolicitada = indiceExistente >= 0
            ? Carrito[indiceExistente].Cantidad + 1
            : 1;

        if (stockDisponible < cantidadSolicitada)
        {
            MostrarErrorEstado(
                $"Stock insuficiente para '{producto.Nombre}'. Disponible en esta sucursal: {stockDisponible}.");
            return;
        }

        if (indiceExistente >= 0)
        {
            var actual = Carrito[indiceExistente];
            Carrito[indiceExistente] = new DetalleVenta
            {
                IdProducto = actual.IdProducto,
                Producto = actual.Producto,
                Cantidad = cantidadSolicitada,
                PrecioUnitario = actual.PrecioUnitario,
                Subtotal = cantidadSolicitada * actual.PrecioUnitario
            };
        }
        else
        {
            Carrito.Add(new DetalleVenta
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
    private async Task IncrementarCantidadAsync(DetalleVenta? detalle)
    {
        if (detalle is null)
            return;

        LimpiarMensaje();
        var indice = Carrito.IndexOf(detalle);
        if (indice < 0)
            return;

        var stockDisponible = await _productoRepository.ObtenerStockAsync(
            detalle.IdProducto, _sesionService.IdSucursalActual);

        var nuevaCantidad = detalle.Cantidad + 1;
        if (stockDisponible < nuevaCantidad)
        {
            MostrarErrorEstado(
                $"Stock máximo alcanzado para '{detalle.Producto?.Nombre}' ({stockDisponible} disponibles).");
            SolicitarFocoEscaneador?.Invoke();
            return;
        }

        Carrito[indice] = new DetalleVenta
        {
            IdProducto = detalle.IdProducto,
            Producto = detalle.Producto,
            Cantidad = nuevaCantidad,
            PrecioUnitario = detalle.PrecioUnitario,
            Subtotal = nuevaCantidad * detalle.PrecioUnitario
        };

        SolicitarFocoEscaneador?.Invoke();
    }

    [RelayCommand]
    private void DisminuirCantidad(DetalleVenta? detalle)
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
            Carrito[indice] = new DetalleVenta
            {
                IdProducto = detalle.IdProducto,
                Producto = detalle.Producto,
                Cantidad = nuevaCantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                Subtotal = nuevaCantidad * detalle.PrecioUnitario
            };
        }

        SolicitarFocoEscaneador?.Invoke();
    }

    [RelayCommand]
    private void EliminarDelCarrito(DetalleVenta? detalle)
    {
        if (detalle is null)
            return;

        LimpiarMensaje();
        Carrito.Remove(detalle);
        SolicitarFocoEscaneador?.Invoke();
    }

    [RelayCommand]
    private void VaciarCarrito()
    {
        LimpiarPantalla();
        SolicitarFocoEscaneador?.Invoke();
    }

    [RelayCommand]
    private void SeleccionarEfectivo() => MetodoPagoSeleccionado = MetodosPago.Efectivo;

    [RelayCommand]
    private void SeleccionarQr() => MetodoPagoSeleccionado = MetodosPago.QR;

    [RelayCommand]
    private void SeleccionarCredito() => MetodoPagoSeleccionado = MetodosPago.Credito;

    // Botones rápidos de billetes (10, 20, 50, 100 y 200 Bs)
    [RelayCommand]
    private void Sumar10() => SumarBillete(10m);

    [RelayCommand]
    private void Sumar20() => SumarBillete(20m);

    [RelayCommand]
    private void Sumar50() => SumarBillete(50m);

    [RelayCommand]
    private void Sumar100() => SumarBillete(100m);

    [RelayCommand]
    private void Sumar200() => SumarBillete(200m);

    [RelayCommand]
    private void MontoExacto()
    {
        MontoRecibido = Total;
        SolicitarFocoEscaneador?.Invoke();
    }

    [RelayCommand]
    private void LimpiarMonto()
    {
        MontoRecibido = 0m;
        SolicitarFocoEscaneador?.Invoke();
    }

    private void SumarBillete(decimal billete)
    {
        LimpiarMensaje();
        MontoRecibido += billete;
        SolicitarFocoEscaneador?.Invoke();
    }

    private bool PuedeCompletarVenta() => !EstaProcesando && Carrito.Count > 0;

    [RelayCommand(CanExecute = nameof(PuedeCompletarVenta))]
    private async Task CompletarVentaAsync()
    {
        LimpiarMensaje();

        if (Carrito.Count == 0)
        {
            MostrarErrorEstado("Agregue al menos un producto al carrito antes de cobrar.");
            return;
        }

        if (!_sesionService.EstaAutenticado)
        {
            MostrarErrorEstado("No existe una sesión de cajero activa. Vuelva a iniciar sesión.");
            return;
        }

        if (EsPagoEfectivo && MontoRecibido < Total)
        {
            MostrarErrorEstado(
                $"El monto recibido (Bs {MontoRecibido:N2}) es insuficiente para cubrir el total (Bs {Total:N2}).");
            return;
        }

        if (IsPagoCredito && ClienteSeleccionado is null)
        {
            MostrarErrorEstado(
                "Para procesar una venta a Crédito es obligatorio confirmar primero al cliente con el botón 'Confirmar Cliente'.");
            return;
        }

        if (VendedorSeleccionado is null)
        {
            MostrarErrorEstado(
                "Seleccione al vendedor que concretó el trato antes de completar la venta.");
            return;
        }

        EstaProcesando = true;
        try
        {
            int? idCliente = IsPagoCredito ? ClienteSeleccionado!.Id : ClienteSeleccionado?.Id;
            var nombreClienteCredito = ClienteSeleccionado?.NombreCompleto;
            var vendedorActual = VendedorSeleccionado;
            var nombreVendedor = vendedorActual.NombreCompleto;
            var clienteRecibo = ClienteSeleccionado ?? new Cliente
            {
                NombreCompleto = string.IsNullOrWhiteSpace(NombreClienteNuevo) ? "SIN NOMBRE" : NombreClienteNuevo.Trim(),
                Ci = string.IsNullOrWhiteSpace(CiNitBusqueda) ? "0" : CiNitBusqueda.Trim()
            };
            var mapaProductosCarrito = Carrito
                .Where(c => c.Producto is not null)
                .GroupBy(c => c.IdProducto)
                .ToDictionary(g => g.Key, g => g.First().Producto!);

            var nuevaVenta = new Venta
            {
                IdUsuario = _sesionService.IdUsuarioActual,
                IdSucursal = _sesionService.IdSucursalActual,
                IdCliente = idCliente,
                IdEmpleado = VendedorSeleccionado.Id,
                MetodoPago = MetodoPagoSeleccionado,
                MontoRecibido = EsPagoEfectivo ? MontoRecibido : (EsPagoQr ? Total : 0m),
                Detalles = Carrito.Select(item => new DetalleVenta
                {
                    IdProducto = item.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = item.Subtotal
                }).ToList()
            };

            // PASO 18: usa la fecha seleccionada por el cajero en el CalendarDatePicker (normalizada a UTC)
            DateTime? vencimientoCreditoUtc = null;
            if (IsPagoCredito)
            {
                var fechaSeleccionada = (FechaLimiteCredito ?? DateTime.Today.AddDays(30)).Date;
                vencimientoCreditoUtc = DateTime.SpecifyKind(fechaSeleccionada, DateTimeKind.Local).ToUniversalTime();
            }

            var ventaGuardada = await _ventaRepository.RegistrarVentaAsync(nuevaVenta, vencimientoCreditoUtc);

            // Rehidratar Producto en cada detalle para que el PDF muestre el nombre real del concepto
            foreach (var det in ventaGuardada.Detalles)
            {
                if (det.Producto is null && mapaProductosCarrito.TryGetValue(det.IdProducto, out var prod))
                {
                    det.Producto = prod;
                }
            }

            var cambioMostrar = ventaGuardada.CambioDado;
            var totalMostrar = ventaGuardada.Total;
            var eraCredito = IsPagoCredito;

            LimpiarPantalla();
            await CargarCatalogoAsync();

            if (eraCredito)
            {
                MostrarExitoEstado(
                    $"¡Venta a Crédito #{ventaGuardada.Id} registrada para '{nombreClienteCredito}' por Bs {totalMostrar:N2} (Vendedor: {nombreVendedor})!");
            }
            else
            {
                var detalleCambio = cambioMostrar > 0m ? $"  •  Cambio: Bs {cambioMostrar:N2}" : string.Empty;
                MostrarExitoEstado(
                    $"¡Venta #{ventaGuardada.Id} completada! Total: Bs {totalMostrar:N2}{detalleCambio}  •  Vendedor: {nombreVendedor}");
            }

            // PASO 23.1: Selección de destino, generación y apertura nativa del PDF en Windows 11
            try
            {
                var topLevel = ObtenerTopLevelActivo();
                if (topLevel?.StorageProvider is not null)
                {
                    var archivoDestino = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Guardar Nota de Venta (PDF)",
                        SuggestedFileName = $"Recibo_{ventaGuardada.Id:D4}.pdf",
                        DefaultExtension = "pdf",
                        ShowOverwritePrompt = true,
                        FileTypeChoices = new[]
                        {
                            new FilePickerFileType("Documento PDF")
                            {
                                Patterns = new[] { "*.pdf" },
                                MimeTypes = new[] { "application/pdf" }
                            }
                        }
                    });

                    if (archivoDestino is not null)
                    {
                        string rutaLocal = archivoDestino.TryGetLocalPath()
                                           ?? archivoDestino.Path.LocalPath;

                        if (!rutaLocal.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            rutaLocal += ".pdf";
                        }

                        await Task.Run(() =>
                            _ticketPdfService.GenerarNotaDeVentaPdf(ventaGuardada, vendedorActual, clienteRecibo, rutaLocal));

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = rutaLocal,
                            UseShellExecute = true
                        });

                        MostrarToast("Venta registrada y recibo generado", NotificationType.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarToast("Error al generar/abrir el PDF: " + ex.Message, NotificationType.Error);
            }

            SolicitarFocoEscaneador?.Invoke();
        }
        catch (StockInsuficienteException ex)
        {
            var nombreProd = Carrito.FirstOrDefault(c => c.IdProducto == ex.IdProducto)?.Producto?.Nombre
                             ?? $"ID {ex.IdProducto}";
            MostrarErrorEstado($"Stock insuficiente en almacén para '{nombreProd}'. Verifique existencias.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo registrar la venta: {ex.Message}");
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
        CompletarVentaCommand.NotifyCanExecuteChanged();

        RecalcularCambio();
    }

    private void RecalcularCambio()
    {
        if (EsPagoEfectivo && Total > 0m && MontoRecibido >= Total)
        {
            CambioDado = MontoRecibido - Total;
        }
        else
        {
            CambioDado = 0m;
        }
    }

    private void LimpiarPantalla()
    {
        Carrito.Clear();
        TextoBusqueda = string.Empty;
        MontoRecibido = 0m;
        MetodoPagoSeleccionado = MetodosPago.Efectivo;
        CiNitBusqueda = string.Empty;
        NombreClienteNuevo = string.Empty;
        CelularClienteNuevo = string.Empty;
        ClienteSeleccionado = null;
        FechaLimiteCredito = DateTime.Today.AddDays(30); // PASO 18: resetear al plazo estándar
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
                "Caja / POS",
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
