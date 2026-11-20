using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class LogisticaViewModel : ViewModelBase
{
    private readonly IDbContextFactory<FerreteriaDbContext> _dbContextFactory;
    private readonly ISesionService _sesionService;

    [ObservableProperty]
    private int _tabSeleccionadoIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CostosOperativosTexto))]
    private decimal _costosOperativosFleteAcumulados;

    [ObservableProperty]
    private int _cargasEnTransitoCount;

    [ObservableProperty]
    private int _despachosActivosCount;

    // =========================================================
    // MODAL / PANEL DE CONFIRMACIÓN AL PASAR CARGA A "RECIBIDO"
    // =========================================================
    [ObservableProperty]
    private bool _mostrarConfirmacionRecepcion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResumenCargaARecibirTexto))]
    [NotifyPropertyChangedFor(nameof(FleteCargaARecibirTexto))]
    private CargaEntrante? _cargaPendienteRecepcion;

    [ObservableProperty]
    private bool _actualizarStockAutomaticamente = true;

    [ObservableProperty]
    private Producto? _productoRecepcionSeleccionado;

    [ObservableProperty]
    private string _cantidadRecibidaTexto = "0";

    [ObservableProperty]
    private string _observacionesRecepcionTexto = string.Empty;

    // =========================================================
    // PANEL DE REGISTRO DE DESPACHO PARCIAL A OBRA
    // =========================================================
    [ObservableProperty]
    private bool _mostrarPanelDespachoParcial;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResumenOrdenParcialTexto))]
    private OrdenDespacho? _ordenParaDespachoParcial;

    [ObservableProperty]
    private string _cantidadDespachoParcialTexto = "1";

    [ObservableProperty]
    private string _nombreReceptorObraInput = string.Empty;

    // =========================================================
    // FORMULARIO: NUEVA CARGA ENTRANTE (PROVEEDOR / FLOTA)
    // =========================================================
    [ObservableProperty]
    private bool _mostrarFormularioNuevaCarga;

    [ObservableProperty]
    private Proveedor? _proveedorNuevaCarga;

    [ObservableProperty]
    private Producto? _productoNuevaCarga;

    [ObservableProperty]
    private string _numeroGuiaForm = string.Empty;

    [ObservableProperty]
    private string _empresaTransporteForm = string.Empty;

    [ObservableProperty]
    private string _costoFleteDestinoForm = "0.00";

    [ObservableProperty]
    private string _cantidadUnidadesCargaForm = "10";

    [ObservableProperty]
    private string _diasEstimadosLlegadaForm = "2";

    [ObservableProperty]
    private string _observacionesCargaForm = string.Empty;

    // =========================================================
    // FORMULARIO: NUEVA ORDEN DE DESPACHO A OBRA
    // =========================================================
    [ObservableProperty]
    private bool _mostrarFormularioNuevoDespacho;

    [ObservableProperty]
    private Venta? _ventaSeleccionadaDespacho;

    [ObservableProperty]
    private string _direccionEntregaForm = string.Empty;

    [ObservableProperty]
    private string _vehiculoChoferForm = "Propio - Camioneta Ferretería";

    [ObservableProperty]
    private string _costoFleteFerreteriaForm = "0.00";

    [ObservableProperty]
    private string _nombreReceptorForm = string.Empty;

    [ObservableProperty]
    private string _unidadesTotalesDespachoForm = "10";

    // Estado y alertas
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ControlesHabilitados))]
    private bool _estaOcupado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private string? _mensajeEstado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    [NotifyPropertyChangedFor(nameof(MostrarExito))]
    private bool _esMensajeExito;

    public LogisticaViewModel(
        IDbContextFactory<FerreteriaDbContext> dbContextFactory,
        ISesionService sesionService)
    {
        _dbContextFactory = dbContextFactory;
        _sesionService = sesionService;

        _ = InicializarAsync();
    }

    public ObservableCollection<CargaEntrante> CargasEntrantes { get; } = new();

    public ObservableCollection<OrdenDespacho> OrdenesDespacho { get; } = new();

    public ObservableCollection<Proveedor> ProveedoresDisponibles { get; } = new();

    public ObservableCollection<Producto> ProductosDisponibles { get; } = new();

    public ObservableCollection<Venta> VentasRecientes { get; } = new();

    public bool SinCargasEntrantes => CargasEntrantes.Count == 0;

    public bool SinOrdenesDespacho => OrdenesDespacho.Count == 0;

    public bool ControlesHabilitados => !EstaOcupado;

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsMensajeExito;

    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsMensajeExito;

    public string NombreSucursalActiva =>
        _sesionService.UsuarioActual?.Sucursal?.Nombre ?? "Sucursal Activa";

    public string CostosOperativosTexto => $"Bs. {CostosOperativosFleteAcumulados:N2}";

    public string ResumenCargaARecibirTexto =>
        CargaPendienteRecepcion is null
            ? string.Empty
            : $"Guía #{CargaPendienteRecepcion.NumeroGuia} • {CargaPendienteRecepcion.NombreProveedorMostrar} ({CargaPendienteRecepcion.EmpresaTransporte})";

    public string FleteCargaARecibirTexto =>
        CargaPendienteRecepcion is null
            ? "Bs. 0.00"
            : $"Bs. {CargaPendienteRecepcion.CostoFleteDestino:N2} (se sumará a Costos Operativos de Flete)";

    public string ResumenOrdenParcialTexto =>
        OrdenParaDespachoParcial is null
            ? string.Empty
            : $"Orden #{OrdenParaDespachoParcial.Id} • Obra: {OrdenParaDespachoParcial.DireccionEntrega} (Pendientes: {OrdenParaDespachoParcial.UnidadesPendientes} u.)";

    partial void OnVentaSeleccionadaDespachoChanged(Venta? value)
    {
        if (value is null)
            return;

        var totalUnidades = value.Detalles?.Sum(d => d.Cantidad) ?? 0;
        if (totalUnidades > 0)
        {
            UnidadesTotalesDespachoForm = totalUnidades.ToString(CultureInfo.InvariantCulture);
        }

        if (value.Cliente is not null && string.IsNullOrWhiteSpace(NombreReceptorForm))
        {
            NombreReceptorForm = value.Cliente.NombreCompleto;
        }
    }

    private async Task InicializarAsync()
    {
        await RecargarTodoAsync();
    }

    [RelayCommand]
    public async Task RecargarTodoAsync()
    {
        LimpiarMensaje();
        EstaOcupado = true;
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            // 1. Cargar Cargas Entrantes (Proveedores -> Ferretería)
            var cargas = await db.CargasEntrantes
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Producto)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            CargasEntrantes.Clear();
            foreach (var c in cargas)
                CargasEntrantes.Add(c);

            // 2. Cargar Órdenes de Despacho (Ferretería -> Obras)
            var despachos = await db.OrdenesDespacho
                .AsNoTracking()
                .Include(o => o.Venta)
                    .ThenInclude(v => v.Cliente)
                .Include(o => o.Venta)
                    .ThenInclude(v => v.Detalles)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            OrdenesDespacho.Clear();
            foreach (var d in despachos)
                OrdenesDespacho.Add(d);

            // 3. Cargar Catálogos Auxiliares para Formularios
            var proveedores = await db.Proveedores
                .AsNoTracking()
                .OrderBy(p => p.NombreEmpresa)
                .ToListAsync();

            ProveedoresDisponibles.Clear();
            foreach (var prov in proveedores)
                ProveedoresDisponibles.Add(prov);

            var productos = await db.Productos
                .AsNoTracking()
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ProductosDisponibles.Clear();
            foreach (var prod in productos)
                ProductosDisponibles.Add(prod);

            var ventas = await db.Ventas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                .OrderByDescending(v => v.FechaHora)
                .Take(30)
                .ToListAsync();

            VentasRecientes.Clear();
            foreach (var v in ventas)
                VentasRecientes.Add(v);

            ProveedorNuevaCarga ??= ProveedoresDisponibles.FirstOrDefault();
            ProductoNuevaCarga ??= ProductosDisponibles.FirstOrDefault();
            VentaSeleccionadaDespacho ??= VentasRecientes.FirstOrDefault();

            RecalcularMetricasLogistica();
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al consultar datos logísticos: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void RecalcularMetricasLogistica()
    {
        var fletesCargasRecibidas = CargasEntrantes
            .Where(c => c.EsRecibido)
            .Sum(c => c.CostoFleteDestino);

        var fletesDespachosObra = OrdenesDespacho
            .Where(o => !o.EsPendiente)
            .Sum(o => o.CostoFleteFerreteria);

        CostosOperativosFleteAcumulados = fletesCargasRecibidas + fletesDespachosObra;
        CargasEnTransitoCount = CargasEntrantes.Count(c => !c.EsRecibido);
        DespachosActivosCount = OrdenesDespacho.Count(o => !o.EsEntregadoCompleto);

        OnPropertyChanged(nameof(SinCargasEntrantes));
        OnPropertyChanged(nameof(SinOrdenesDespacho));
    }

    // =========================================================
    // GESTIÓN DE CARGAS ENTRANTES (LLEGADAS DE PROVEEDORES)
    // =========================================================

    [RelayCommand]
    private void AbrirFormularioNuevaCarga()
    {
        LimpiarMensaje();
        NumeroGuiaForm = string.Empty;
        EmpresaTransporteForm = "Flota Expreso Tarija";
        CostoFleteDestinoForm = "0.00";
        CantidadUnidadesCargaForm = "20";
        DiasEstimadosLlegadaForm = "2";
        ObservacionesCargaForm = string.Empty;
        ProveedorNuevaCarga = ProveedoresDisponibles.FirstOrDefault();
        ProductoNuevaCarga = ProductosDisponibles.FirstOrDefault();

        MostrarFormularioNuevaCarga = true;
    }

    [RelayCommand]
    private void CerrarFormularioNuevaCarga() => MostrarFormularioNuevaCarga = false;

    [RelayCommand]
    private async Task GuardarNuevaCargaAsync()
    {
        LimpiarMensaje();

        if (ProveedorNuevaCarga is null)
        {
            MostrarErrorEstado("Seleccione el proveedor que envía la carga.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NumeroGuiaForm))
        {
            MostrarErrorEstado("Ingrese el número de guía o remito de envío.");
            return;
        }

        if (string.IsNullOrWhiteSpace(EmpresaTransporteForm))
        {
            MostrarErrorEstado("Indique la empresa transportista o flota.");
            return;
        }

        if (!IntentarParsearDecimal(CostoFleteDestinoForm, out var costoFlete) || costoFlete < 0m)
        {
            MostrarErrorEstado("Ingrese un Costo de Flete en Destino válido (mayor o igual a 0).");
            return;
        }

        int.TryParse(CantidadUnidadesCargaForm?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var unidades);
        if (!int.TryParse(DiasEstimadosLlegadaForm?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var dias) || dias < 0)
            dias = 2;

        EstaOcupado = true;
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var nuevaCarga = new CargaEntrante
            {
                IdProveedor = ProveedorNuevaCarga.Id,
                IdSucursal = _sesionService.IdSucursalActual,
                FechaRegistro = DateTime.UtcNow,
                MontoTotal = ProductoNuevaCarga is not null
                    ? ProductoNuevaCarga.PrecioCompra * Math.Max(0, unidades)
                    : 0m,
                IdProducto = ProductoNuevaCarga?.Id,
                CantidadUnidades = Math.Max(0, unidades),
                NumeroGuia = NumeroGuiaForm.Trim(),
                EmpresaTransporte = EmpresaTransporteForm.Trim(),
                CostoFleteDestino = costoFlete,
                FechaEstimadaLlegada = DateTime.UtcNow.AddDays(dias),
                Estado = EstadosCargaEntrante.EnTransito,
                Observaciones = string.IsNullOrWhiteSpace(ObservacionesCargaForm) ? null : ObservacionesCargaForm.Trim()
            };

            db.CargasEntrantes.Add(nuevaCarga);
            await db.SaveChangesAsync();

            MostrarFormularioNuevaCarga = false;
            await RecargarTodoAsync();

            MostrarExitoEstado($"Carga entrante Guía #{nuevaCarga.NumeroGuia} registrada en estado 'En Tránsito'.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo registrar la carga entrante: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    [RelayCommand]
    private async Task MarcarCargaEnTerminalAsync(CargaEntrante? carga)
    {
        if (carga is null)
            return;

        await ActualizarEstadoSimpleCargaAsync(carga, EstadosCargaEntrante.EnTerminal);
    }

    [RelayCommand]
    private async Task MarcarCargaConObservacionAsync(CargaEntrante? carga)
    {
        if (carga is null)
            return;

        await ActualizarEstadoSimpleCargaAsync(carga, EstadosCargaEntrante.ConObservaciones);
    }

    private async Task ActualizarEstadoSimpleCargaAsync(CargaEntrante carga, string nuevoEstado)
    {
        LimpiarMensaje();
        EstaOcupado = true;
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            await db.CargasEntrantes
                .Where(c => c.Id == carga.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Estado, nuevoEstado));

            await RecargarTodoAsync();
            MostrarExitoEstado($"La Guía #{carga.NumeroGuia} pasó al estado '{nuevoEstado}'.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo actualizar el estado de la carga: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    /// <summary>
    /// Cuando una carga entrante pasa a "Recibido", abre el cuadro de confirmación preguntando si se desea
    /// actualizar automáticamente el stock de la sucursal actual y sumar el CostoFleteDestino a los costos operativos.
    /// </summary>
    [RelayCommand]
    private void CambiarEstadoCarga(CargaEntrante? carga)
    {
        if (carga is null)
            return;

        LimpiarMensaje();
        CargaPendienteRecepcion = carga;
        ActualizarStockAutomaticamente = !carga.StockActualizado;
        ProductoRecepcionSeleccionado = ProductosDisponibles.FirstOrDefault(p => p.Id == carga.IdProducto)
                                        ?? ProductosDisponibles.FirstOrDefault();
        CantidadRecibidaTexto = (carga.CantidadUnidades > 0 ? carga.CantidadUnidades : 10)
            .ToString(CultureInfo.InvariantCulture);
        ObservacionesRecepcionTexto = carga.Observaciones ?? string.Empty;

        MostrarConfirmacionRecepcion = true;
    }

    [RelayCommand]
    private void CancelarRecepcionCarga()
    {
        MostrarConfirmacionRecepcion = false;
        CargaPendienteRecepcion = null;
    }

    /// <summary>
    /// Ejecuta en una transacción atómica la recepción de la carga: cambia el estado a "Recibido",
    /// suma el CostoFleteDestino a los costos operativos y, si el usuario lo confirmó, incrementa el stock en la sucursal.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmarRecepcionCargaAsync()
    {
        if (CargaPendienteRecepcion is null)
            return;

        LimpiarMensaje();

        var cantidadRecibida = 0;
        if (ActualizarStockAutomaticamente)
        {
            if (ProductoRecepcionSeleccionado is null)
            {
                MostrarErrorEstado("Seleccione el producto cuyo stock desea incrementar automáticamente.");
                return;
            }

            if (!int.TryParse(CantidadRecibidaTexto?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out cantidadRecibida)
                || cantidadRecibida <= 0)
            {
                MostrarErrorEstado("Ingrese una cantidad de unidades recibidas mayor a 0 para actualizar el inventario.");
                return;
            }
        }

        EstaOcupado = true;
        try
        {
            var idCarga = CargaPendienteRecepcion.Id;
            var idSucursal = _sesionService.IdSucursalActual;

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            await using var transaccion = await db.Database.BeginTransactionAsync();

            var entidadCarga = await db.CargasEntrantes.FirstOrDefaultAsync(c => c.Id == idCarga);
            if (entidadCarga is null)
                throw new InvalidOperationException("La guía de carga ya no existe en la base de datos.");

            entidadCarga.Estado = EstadosCargaEntrante.Recibido;
            if (!string.IsNullOrWhiteSpace(ObservacionesRecepcionTexto))
            {
                entidadCarga.Observaciones = ObservacionesRecepcionTexto.Trim();
            }

            string detalleStockMensaje = "sin modificar inventario";

            if (ActualizarStockAutomaticamente && !entidadCarga.StockActualizado && ProductoRecepcionSeleccionado is not null)
            {
                var idProd = ProductoRecepcionSeleccionado.Id;
                entidadCarga.IdProducto = idProd;
                entidadCarga.CantidadUnidades = cantidadRecibida;
                entidadCarga.StockActualizado = true;

                var stockSucursal = await db.StockSucursales
                    .FirstOrDefaultAsync(s => s.IdProducto == idProd && s.IdSucursal == idSucursal);

                if (stockSucursal is not null)
                {
                    stockSucursal.CantidadActual += cantidadRecibida;
                }
                else
                {
                    db.StockSucursales.Add(new StockSucursal
                    {
                        IdProducto = idProd,
                        IdSucursal = idSucursal,
                        CantidadActual = cantidadRecibida
                    });
                }

                detalleStockMensaje = $"+{cantidadRecibida} u. sumadas al stock de '{ProductoRecepcionSeleccionado.Nombre}'";
            }

            await db.SaveChangesAsync();
            await transaccion.CommitAsync();

            var fleteSumado = entidadCarga.CostoFleteDestino;
            var guiaMostrar = entidadCarga.NumeroGuia;

            MostrarConfirmacionRecepcion = false;
            CargaPendienteRecepcion = null;

            await RecargarTodoAsync();

            MostrarExitoEstado(
                $"Guía #{guiaMostrar} marcada como 'Recibido' ({detalleStockMensaje}). Flete de Bs {fleteSumado:N2} sumado a costos operativos.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al confirmar la recepción de carga: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // =========================================================
    // GESTIÓN DE ÓRDENES DE DESPACHO A OBRAS (ENVÍOS)
    // =========================================================

    [RelayCommand]
    private void AbrirFormularioNuevoDespacho()
    {
        LimpiarMensaje();
        VentaSeleccionadaDespacho = VentasRecientes.FirstOrDefault();
        DireccionEntregaForm = string.Empty;
        VehiculoChoferForm = "Propio - Camioneta Ferretería";
        CostoFleteFerreteriaForm = "50.00";
        NombreReceptorForm = VentaSeleccionadaDespacho?.Cliente?.NombreCompleto ?? string.Empty;

        var unidadesVenta = VentaSeleccionadaDespacho?.Detalles?.Sum(d => d.Cantidad) ?? 10;
        UnidadesTotalesDespachoForm = Math.Max(1, unidadesVenta).ToString(CultureInfo.InvariantCulture);

        MostrarFormularioNuevoDespacho = true;
    }

    [RelayCommand]
    private void CerrarFormularioNuevoDespacho() => MostrarFormularioNuevoDespacho = false;

    [RelayCommand]
    private async Task GuardarNuevoDespachoAsync()
    {
        LimpiarMensaje();

        if (VentaSeleccionadaDespacho is null)
        {
            MostrarErrorEstado("Seleccione la Venta asociada al despacho a obra.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DireccionEntregaForm))
        {
            MostrarErrorEstado("Ingrese la dirección de la obra o lugar de entrega.");
            return;
        }

        if (string.IsNullOrWhiteSpace(VehiculoChoferForm))
        {
            MostrarErrorEstado("Indique el vehículo y chofer asignado (Propio o Externo).");
            return;
        }

        if (!IntentarParsearDecimal(CostoFleteFerreteriaForm, out var costoFlete) || costoFlete < 0m)
        {
            MostrarErrorEstado("Ingrese un costo de flete válido.");
            return;
        }

        if (!int.TryParse(UnidadesTotalesDespachoForm?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var unidadesTotales)
            || unidadesTotales <= 0)
        {
            unidadesTotales = 1;
        }

        EstaOcupado = true;
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var nuevaOrden = new OrdenDespacho
            {
                IdVenta = VentaSeleccionadaDespacho.Id,
                DireccionEntrega = DireccionEntregaForm.Trim(),
                VehiculoChofer = VehiculoChoferForm.Trim(),
                CostoFleteFerreteria = costoFlete,
                Estado = EstadosOrdenDespacho.Pendiente,
                NombreReceptorObra = string.IsNullOrWhiteSpace(NombreReceptorForm) ? null : NombreReceptorForm.Trim(),
                UnidadesTotales = unidadesTotales,
                UnidadesEntregadas = 0
            };

            db.OrdenesDespacho.Add(nuevaOrden);
            await db.SaveChangesAsync();

            MostrarFormularioNuevoDespacho = false;
            await RecargarTodoAsync();

            MostrarExitoEstado($"Orden de despacho #{nuevaOrden.Id} creada para '{nuevaOrden.DireccionEntrega}'.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo crear la orden de despacho: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    [RelayCommand]
    private async Task MarcarDespachoEnRutaAsync(OrdenDespacho? orden)
    {
        if (orden is null)
            return;

        LimpiarMensaje();
        EstaOcupado = true;
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            await db.OrdenesDespacho
                .Where(o => o.Id == orden.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Estado, EstadosOrdenDespacho.EnRuta));

            await RecargarTodoAsync();
            MostrarExitoEstado($"Orden #{orden.Id} marcada 'En Ruta' hacia {orden.DireccionEntrega}.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo actualizar el despacho: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    [RelayCommand]
    private void AbrirPanelDespachoParcial(OrdenDespacho? orden)
    {
        if (orden is null)
            return;

        LimpiarMensaje();
        OrdenParaDespachoParcial = orden;
        CantidadDespachoParcialTexto = Math.Max(1, orden.UnidadesPendientes).ToString(CultureInfo.InvariantCulture);
        NombreReceptorObraInput = orden.NombreReceptorObra ?? string.Empty;

        MostrarPanelDespachoParcial = true;
    }

    [RelayCommand]
    private void CancelarDespachoParcial()
    {
        MostrarPanelDespachoParcial = false;
        OrdenParaDespachoParcial = null;
    }

    /// <summary>
    /// Descuenta las cantidades entregadas en una obra sin cerrar toda la orden (salvo que se complete el 100% de unidades).
    /// </summary>
    [RelayCommand]
    private async Task RegistrarDespachoParcialAsync()
    {
        if (OrdenParaDespachoParcial is null)
            return;

        LimpiarMensaje();

        if (!int.TryParse(CantidadDespachoParcialTexto?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var cantidadEntregada)
            || cantidadEntregada <= 0)
        {
            MostrarErrorEstado("Ingrese una cantidad de unidades entregadas mayor a 0.");
            return;
        }

        EstaOcupado = true;
        try
        {
            var idOrden = OrdenParaDespachoParcial.Id;

            await using var db = await _dbContextFactory.CreateDbContextAsync();
            await using var transaccion = await db.Database.BeginTransactionAsync();

            var ordenDb = await db.OrdenesDespacho.FirstOrDefaultAsync(o => o.Id == idOrden);
            if (ordenDb is null)
                throw new InvalidOperationException("La orden de despacho ya no existe.");

            var nuevoAcumulado = Math.Min(ordenDb.UnidadesTotales, ordenDb.UnidadesEntregadas + cantidadEntregada);
            ordenDb.UnidadesEntregadas = nuevoAcumulado;

            if (!string.IsNullOrWhiteSpace(NombreReceptorObraInput))
            {
                ordenDb.NombreReceptorObra = NombreReceptorObraInput.Trim();
            }

            ordenDb.FechaEntrega = DateTime.UtcNow;

            if (ordenDb.UnidadesEntregadas >= ordenDb.UnidadesTotales)
            {
                ordenDb.Estado = EstadosOrdenDespacho.EntregadoCompleto;
            }
            else
            {
                ordenDb.Estado = EstadosOrdenDespacho.EntregadoParcial;
            }

            await db.SaveChangesAsync();
            await transaccion.CommitAsync();

            var estadoFinal = ordenDb.Estado;
            var pendientesRestantes = Math.Max(0, ordenDb.UnidadesTotales - ordenDb.UnidadesEntregadas);

            MostrarPanelDespachoParcial = false;
            OrdenParaDespachoParcial = null;

            await RecargarTodoAsync();

            MostrarExitoEstado(
                $"Entrega en obra registrada (+{cantidadEntregada} u.). Estado: '{estadoFinal}' ({pendientesRestantes} u. pendientes).");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo registrar el despacho parcial: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    [RelayCommand]
    private async Task MarcarDespachoEntregadoCompletoAsync(OrdenDespacho? orden)
    {
        if (orden is null)
            return;

        LimpiarMensaje();
        EstaOcupado = true;
        try
        {
            var ahoraUtc = DateTime.UtcNow;
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            await db.OrdenesDespacho
                .Where(o => o.Id == orden.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Estado, EstadosOrdenDespacho.EntregadoCompleto)
                    .SetProperty(x => x.UnidadesEntregadas, x => x.UnidadesTotales)
                    .SetProperty(x => x.FechaEntrega, ahoraUtc));

            await RecargarTodoAsync();
            MostrarExitoEstado($"Orden #{orden.Id} entregada al 100% en '{orden.DireccionEntrega}'.");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo cerrar la entrega: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private static bool IntentarParsearDecimal(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        var limpio = texto.Trim().Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
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

