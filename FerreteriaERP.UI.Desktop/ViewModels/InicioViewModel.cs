using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Services;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.UI.Desktop.ViewModels;

/// <summary>
/// Representa uno de los productos más vendidos del mes en el ranking Top 7 de Inicio.
/// </summary>
public record ProductoTop(
    string Nombre,
    string? ImagenUrl,
    int CantidadVendida,
    decimal MontoGenerado)
{
    public string CantidadVendidaTexto => CantidadVendida == 1
        ? "1 u. vendida"
        : $"{CantidadVendida} u. vendidas";

    public string MontoGeneradoTexto => $"Bs. {MontoGenerado:N2}";
}

/// <summary>
/// Representa una alerta compacta de stock en la columna derecha de Inicio ("Agotado" o "Bajo").
/// </summary>
public record ProductoAlerta(
    string Nombre,
    int StockActual,
    string TipoAlerta)
{
    public bool EsAgotado => string.Equals(TipoAlerta, "Agotado", StringComparison.OrdinalIgnoreCase);

    public bool EsBajo => !EsAgotado;

    public string BadgeTexto => EsAgotado
        ? "Agotado (0)"
        : $"Bajo ({StockActual})";
}

/// <summary>
/// Representa una alerta de crédito próximo a vencer (≤ 3 días) o ya vencido.
/// </summary>
public record CreditoAlerta(
    string NombreCliente,
    decimal MontoDeuda,
    int DiasRestantes,
    string Telefono)
{
    public string MontoDeudaTexto => $"Bs. {MontoDeuda:N2}";

    public bool EstaVencido => DiasRestantes < 0;

    public bool PorVencerPronto => DiasRestantes >= 0;

    public string EtiquetaVencimiento => DiasRestantes switch
    {
        < 0 => "¡Vencido!",
        0 => "Vence hoy",
        1 => "Vence en 1 día",
        _ => $"Vence en {DiasRestantes} días"
    };

    public string TelefonoTexto => string.IsNullOrWhiteSpace(Telefono)
        ? "Sin teléfono"
        : $"Tel: {Telefono}";
}

/// <summary>
/// ViewModel de la Pantalla de Inicio (PASO 17) con métricas operativas, Top 7 Productos,
/// Alertas de Inventario y Próximos Vencimientos de Crédito.
/// </summary>
public partial class InicioViewModel : ViewModelBase
{
    private const string EstadoCreditoPendiente = "Pendiente";

    private readonly IDbContextFactory<FerreteriaDbContext> _dbContextFactory;
    private readonly ISesionService _sesionService;
    private readonly IPromocionRepository _promocionRepository;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VentasDelDiaTexto))]
    private decimal _ventasDelDia;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VentasDelMesTexto))]
    private decimal _ventasDelMes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalCreditosPorCobrarTexto))]
    private decimal _totalCreditosPorCobrar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientesHoyTexto))]
    [NotifyPropertyChangedFor(nameof(DetalleClientesHoyTexto))]
    private int _clientesHoy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LlegadasPendientesTexto))]
    [NotifyPropertyChangedFor(nameof(DetalleLlegadasPendientesTexto))]
    private int _llegadasPendientes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DespachosPendientesTexto))]
    [NotifyPropertyChangedFor(nameof(DetalleDespachosPendientesTexto))]
    private int _despachosPendientes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetalleVentasHoyTexto))]
    [NotifyPropertyChangedFor(nameof(DetalleClientesHoyTexto))]
    private int _cantidadVentasHoy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetalleVentasMesTexto))]
    private int _cantidadVentasMes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetalleCreditosPendientesTexto))]
    private int _cantidadCreditosPendientes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetalleCreditosPendientesTexto))]
    private int _cantidadCreditosVencidos;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TienePromocionVigente))]
    private Promocion _promocionVigente = CrearPromocionPorDefecto();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ControlesHabilitados))]
    [NotifyPropertyChangedFor(nameof(TextoBotonActualizar))]
    [NotifyPropertyChangedFor(nameof(SinAlertasInventario))]
    [NotifyPropertyChangedFor(nameof(SinTopProductos))]
    [NotifyPropertyChangedFor(nameof(SinCreditosPorVencer))]
    private bool _estaCargando;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    private string? _mensajeError;

    [ObservableProperty]
    private string _ultimaActualizacionTexto = "Actualizando métricas...";

    public InicioViewModel(
        IDbContextFactory<FerreteriaDbContext> dbContextFactory,
        ISesionService sesionService,
        IPromocionRepository promocionRepository)
    {
        _dbContextFactory = dbContextFactory;
        _sesionService = sesionService;
        _promocionRepository = promocionRepository;

        PromocionVigente = CrearPromocionPorDefecto();

        _ = CargarEstadisticasAsync();
    }

    /// <summary>
    /// Callback inyectado desde <see cref="MainViewModel"/> para navegar al módulo de Promociones.
    /// </summary>
    public Action? OnNavegarAPromociones { get; set; }

    /// <summary>
    /// Callback inyectado desde <see cref="MainViewModel"/> para navegar al módulo de Reabastecimiento (PASO 30).
    /// </summary>
    public Action? OnNavegarAReabastecimiento { get; set; }

    /// <summary>
    /// Visibilidad del botón "Ver promociones ->" acorde a la política de roles del menú lateral.
    /// </summary>
    public bool PuedeVerPromociones => SesionActual.EsSuperAdmin;

    /// <summary>
    /// Visibilidad del botón "Reabastecer ->" para roles autorizados (Admin / SuperAdmin / Almacenero).
    /// </summary>
    public bool PuedeVerReabastecimiento => SesionActual.PuedeGestionarReabastecimiento;

    [RelayCommand]
    private void VerPromociones()
    {
        OnNavegarAPromociones?.Invoke();
    }

    [RelayCommand]
    private void IrAReabastecimiento()
    {
        OnNavegarAReabastecimiento?.Invoke();
    }

    /// <summary>
    /// Top 7 de productos más vendidos en el mes en curso.
    /// </summary>
    public ObservableCollection<ProductoTop> TopProductos { get; } = new();

    /// <summary>
    /// Lista compacta de alertas de stock ("Agotado" primero, luego "Bajo") para la columna derecha.
    /// </summary>
    public ObservableCollection<ProductoAlerta> AlertasInventario { get; } = new();

    /// <summary>
    /// Créditos con saldo pendiente cuya fecha de vencimiento es hoy, ya venció o vence en ≤ 3 días.
    /// </summary>
    public ObservableCollection<CreditoAlerta> CreditosPorVencer { get; } = new();

    public string SaludoUsuario =>
        _sesionService.UsuarioActual is not null
            ? $"Hola, {_sesionService.UsuarioActual.NombreCompleto}"
            : "Inicio";

    public string NombreSucursalActiva =>
        _sesionService.UsuarioActual?.Sucursal?.Nombre ?? "Sucursal Activa";

    public string RolUsuarioActivo =>
        _sesionService.UsuarioActual?.Rol?.Nombre ?? "Operador";

    public string VentasDelDiaTexto => $"Bs. {VentasDelDia:N2}";

    public string VentasDelMesTexto => $"Bs. {VentasDelMes:N2}";

    public string TotalCreditosPorCobrarTexto => $"Bs. {TotalCreditosPorCobrar:N2}";

    public string ClientesHoyTexto => ClientesHoy.ToString("N0");

    public string LlegadasPendientesTexto => LlegadasPendientes.ToString("N0");

    public string DespachosPendientesTexto => DespachosPendientes.ToString("N0");

    public string DetalleVentasHoyTexto =>
        CantidadVentasHoy == 1
            ? "1 transacción hoy (contado + abonos)"
            : $"{CantidadVentasHoy} transacciones hoy (contado + abonos)";

    public string DetalleVentasMesTexto =>
        CantidadVentasMes == 1
            ? $"1 venta en {DateTime.Now:MMMM} (contado + abonos)"
            : $"{CantidadVentasMes} ventas en {DateTime.Now:MMMM} (contado + abonos)";

    public string DetalleCreditosPendientesTexto =>
        CantidadCreditosVencidos > 0
            ? $"{CantidadCreditosPendientes} activos • {CantidadCreditosVencidos} vencidos"
            : $"{CantidadCreditosPendientes} crédito(s) por cobrar";

    public string DetalleClientesHoyTexto =>
        ClientesHoy == 1
            ? "1 comprador único hoy"
            : $"{ClientesHoy} compradores únicos hoy";

    public string DetalleLlegadasPendientesTexto =>
        LlegadasPendientes == 1
            ? "1 carga en tránsito o terminal"
            : $"{LlegadasPendientes} cargas por recibir";

    public string DetalleDespachosPendientesTexto =>
        DespachosPendientes == 1
            ? "1 entrega pendiente o en ruta"
            : $"{DespachosPendientes} entregas a obra activas";

    public bool TienePromocionVigente => PromocionVigente is not null;

    public bool ControlesHabilitados => !EstaCargando;

    public string TextoBotonActualizar => EstaCargando ? "Actualizando..." : "Actualizar";

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeError);

    public bool SinAlertasInventario => !EstaCargando && AlertasInventario.Count == 0;

    public bool SinTopProductos => !EstaCargando && TopProductos.Count == 0;

    public bool SinCreditosPorVencer => !EstaCargando && CreditosPorVencer.Count == 0;

    /// <summary>
    /// Consulta de forma asíncrona todas las métricas operativas, logística, Top 7 de productos,
    /// alertas de inventario y alertas de vencimientos de crédito.
    /// </summary>
    [RelayCommand]
    public async Task CargarEstadisticasAsync()
    {
        if (EstaCargando)
            return;

        MensajeError = null;

        if (!_sesionService.EstaAutenticado)
        {
            MensajeError = "No hay una sesión activa para consultar estadísticas.";
            return;
        }

        EstaCargando = true;
        try
        {
            var idSucursal = _sesionService.IdSucursalActual;

            var ahoraUtc = DateTime.UtcNow;
            var inicioDiaUtc = new DateTime(ahoraUtc.Year, ahoraUtc.Month, ahoraUtc.Day, 0, 0, 0, DateTimeKind.Utc);
            var finDiaUtc = inicioDiaUtc.AddDays(1);

            var inicioMesUtc = new DateTime(ahoraUtc.Year, ahoraUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var finMesUtc = inicioMesUtc.AddMonths(1);

            var limiteTresDiasUtc = inicioDiaUtc.AddDays(4);

            await using var context = await _dbContextFactory.CreateDbContextAsync();

            // ================================================================
            // 1. INGRESOS DEL DÍA (PASO 18): Ventas al contado + Abonos cobrados hoy
            // ================================================================
            var queryVentasHoy = context.Ventas
                .AsNoTracking()
                .Where(v => v.IdSucursal == idSucursal &&
                            v.FechaHora >= inicioDiaUtc &&
                            v.FechaHora < finDiaUtc);

            // Ventas al contado hoy (Efectivo + QR, excluye Crédito)
            var totalVentasContadoHoy = await queryVentasHoy
                .Where(v => v.MetodoPago != "Credito")
                .SumAsync(v => (decimal?)v.Total) ?? 0m;

            // Abonos de créditos recibidos hoy (caja real)
            var totalAbonosHoy = await context.AbonosCredito
                .AsNoTracking()
                .Where(a => a.FechaPago >= inicioDiaUtc && a.FechaPago < finDiaUtc &&
                            a.Credito.Venta.IdSucursal == idSucursal)
                .SumAsync(a => (decimal?)a.MontoPagado) ?? 0m;

            var totalHoy = totalVentasContadoHoy + totalAbonosHoy;
            var conteoHoy = await queryVentasHoy.CountAsync();

            var clientesRegistradosUnicosHoy = await queryVentasHoy
                .Where(v => v.IdCliente != null)
                .Select(v => v.IdCliente!.Value)
                .Distinct()
                .CountAsync();

            var ventasMostradorSinClienteHoy = await queryVentasHoy
                .CountAsync(v => v.IdCliente == null);

            var totalClientesUnicosHoy = clientesRegistradosUnicosHoy + ventasMostradorSinClienteHoy;

            // ================================================================
            // 2. INGRESOS DEL MES (PASO 18): Ventas al contado + Abonos cobrados este mes
            // ================================================================
            var queryVentasMes = context.Ventas
                .AsNoTracking()
                .Where(v => v.IdSucursal == idSucursal &&
                            v.FechaHora >= inicioMesUtc &&
                            v.FechaHora < finMesUtc);

            var totalVentasContadoMes = await queryVentasMes
                .Where(v => v.MetodoPago != "Credito")
                .SumAsync(v => (decimal?)v.Total) ?? 0m;

            var totalAbonosMes = await context.AbonosCredito
                .AsNoTracking()
                .Where(a => a.FechaPago >= inicioMesUtc && a.FechaPago < finMesUtc &&
                            a.Credito.Venta.IdSucursal == idSucursal)
                .SumAsync(a => (decimal?)a.MontoPagado) ?? 0m;

            var totalMes = totalVentasContadoMes + totalAbonosMes;
            var conteoMes = await queryVentasMes.CountAsync();

            // 3. Cuentas por Cobrar (Créditos Pendientes de la sucursal)
            var queryCreditosPendientes = context.Creditos
                .AsNoTracking()
                .Where(c => c.Estado == EstadoCreditoPendiente &&
                            c.SaldoPendiente > 0m &&
                            c.Venta.IdSucursal == idSucursal);

            var totalPorCobrar = await queryCreditosPendientes.SumAsync(c => (decimal?)c.SaldoPendiente) ?? 0m;
            var conteoCreditosPendientes = await queryCreditosPendientes.CountAsync();
            var conteoCreditosVencidos = await queryCreditosPendientes
                .CountAsync(c => c.FechaVencimiento != null && c.FechaVencimiento <= ahoraUtc);

            // 4. Llegadas Pendientes (CargasEntrantes cuyo Estado != "Recibido")
            var llegadasPendientesCount = 0;
            try
            {
                llegadasPendientesCount = await context.CargasEntrantes
                    .AsNoTracking()
                    .CountAsync(c => c.IdSucursal == idSucursal &&
                                     c.Estado != EstadosCargaEntrante.Recibido);
            }
            catch
            {
                try
                {
                    llegadasPendientesCount = await context.CargasEntrantes
                        .AsNoTracking()
                        .CountAsync(c => c.Estado != EstadosCargaEntrante.Recibido);
                }
                catch
                {
                    llegadasPendientesCount = 0;
                }
            }

            // 5. Despachos Pendientes (OrdenesDespacho asociadas a ventas de la sucursal con Estado "Pendiente" o "En Ruta")
            var despachosPendientesCount = 0;
            try
            {
                despachosPendientesCount = await context.OrdenesDespacho
                    .AsNoTracking()
                    .CountAsync(o => o.Venta.IdSucursal == idSucursal &&
                                     (o.Estado == EstadosOrdenDespacho.Pendiente ||
                                      o.Estado == EstadosOrdenDespacho.EnRuta));
            }
            catch
            {
                despachosPendientesCount = 0;
            }

            // 6. Top 7 Productos Más Vendidos del Mes
            var detallesMes = await context.DetalleVentas
                .AsNoTracking()
                .Include(d => d.Producto)
                .Where(d => d.Venta.IdSucursal == idSucursal &&
                            d.Venta.FechaHora >= inicioMesUtc &&
                            d.Venta.FechaHora < finMesUtc)
                .ToListAsync();

            var listaTopProductos = detallesMes
                .Where(d => d.Producto is not null)
                .GroupBy(d => d.IdProducto)
                .Select(g =>
                {
                    var prod = g.First().Producto!;
                    return new ProductoTop(
                        Nombre: prod.Nombre,
                        ImagenUrl: prod.ImagenUrl,
                        CantidadVendida: g.Sum(x => x.Cantidad),
                        MontoGenerado: g.Sum(x => x.Subtotal));
                })
                .OrderByDescending(p => p.CantidadVendida)
                .ThenByDescending(p => p.MontoGenerado)
                .Take(7)
                .ToList();

            // 7. Alertas de Inventario (StockActual <= StockMinimo: primero "Agotado" == 0, luego "Bajo" > 0)
            var productosConStock = await context.Productos
                .AsNoTracking()
                .Include(p => p.Stocks.Where(s => s.IdSucursal == idSucursal))
                .ToListAsync();

            var listaAlertasInventario = productosConStock
                .Select(p =>
                {
                    var stockActual = Math.Max(0, p.Stocks.FirstOrDefault(s => s.IdSucursal == idSucursal)?.CantidadActual ?? 0);
                    return new
                    {
                        p.Nombre,
                        StockActual = stockActual,
                        p.StockMinimo
                    };
                })
                .Where(x => x.StockActual <= x.StockMinimo)
                .OrderBy(x => x.StockActual == 0 ? 0 : 1)
                .ThenBy(x => x.StockActual)
                .ThenBy(x => x.Nombre)
                .Select(x => new ProductoAlerta(
                    Nombre: x.Nombre,
                    StockActual: x.StockActual,
                    TipoAlerta: x.StockActual == 0 ? "Agotado" : "Bajo"))
                .ToList();

            // 8. Próximos Vencimientos de Crédito (SaldoPendiente > 0 y FechaVencimiento <= Hoy + 3 días)
            var creditosCandidatos = await context.Creditos
                .AsNoTracking()
                .Include(c => c.Cliente)
                .Include(c => c.Venta)
                .Where(c => c.SaldoPendiente > 0m &&
                            c.Venta.IdSucursal == idSucursal &&
                            c.FechaVencimiento != null &&
                            c.FechaVencimiento <= limiteTresDiasUtc)
                .OrderBy(c => c.FechaVencimiento)
                .ToListAsync();

            var hoyLocal = DateTime.Today;
            var listaAlertasCredito = creditosCandidatos
                .Select(c =>
                {
                    var fechaVencLocal = c.FechaVencimiento!.Value.ToLocalTime().Date;
                    var diasRestantes = (int)Math.Round((fechaVencLocal - hoyLocal).TotalDays);
                    return new CreditoAlerta(
                        NombreCliente: c.Cliente?.NombreCompleto ?? $"Cliente #{c.IdCliente}",
                        MontoDeuda: c.SaldoPendiente,
                        DiasRestantes: diasRestantes,
                        Telefono: c.Cliente?.CelularWhatsapp ?? string.Empty);
                })
                .OrderBy(a => a.DiasRestantes)
                .ThenByDescending(a => a.MontoDeuda)
                .ToList();

            // 9. Promoción Vigente (consulta real desde IPromocionRepository)
            Promocion? promoActiva = null;
            try
            {
                var vigentes = await _promocionRepository.ObtenerVigentesAsync(idSucursal, ahoraUtc);
                promoActiva = vigentes.FirstOrDefault();
            }
            catch
            {
                // Si la tabla Promociones aún no fue migrada, muestra el estado sin promociones activas
            }

            // Actualizar propiedades en la UI
            VentasDelDia = totalHoy;
            CantidadVentasHoy = conteoHoy;
            ClientesHoy = totalClientesUnicosHoy;

            VentasDelMes = totalMes;
            CantidadVentasMes = conteoMes;

            TotalCreditosPorCobrar = totalPorCobrar;
            CantidadCreditosPendientes = conteoCreditosPendientes;
            CantidadCreditosVencidos = conteoCreditosVencidos;

            LlegadasPendientes = llegadasPendientesCount;
            DespachosPendientes = despachosPendientesCount;

            PromocionVigente = promoActiva ?? CrearPromocionPorDefecto();

            TopProductos.Clear();
            foreach (var top in listaTopProductos)
            {
                TopProductos.Add(top);
            }

            AlertasInventario.Clear();
            foreach (var alertaInv in listaAlertasInventario)
            {
                AlertasInventario.Add(alertaInv);
            }

            CreditosPorVencer.Clear();
            foreach (var alertaCred in listaAlertasCredito)
            {
                CreditosPorVencer.Add(alertaCred);
            }

            UltimaActualizacionTexto = $"Actualizado hoy a las {DateTime.Now:HH:mm:ss}";
            OnPropertyChanged(nameof(SinAlertasInventario));
            OnPropertyChanged(nameof(SinTopProductos));
            OnPropertyChanged(nameof(SinCreditosPorVencer));
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudieron cargar las estadísticas de Inicio: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private static Promocion CrearPromocionPorDefecto()
    {
        var ahoraUtc = DateTime.UtcNow;
        return new Promocion
        {
            Nombre = "Sin promociones activas",
            Descripcion = "No hay campañas comerciales vigentes en este momento para esta sucursal.",
            FechaInicio = ahoraUtc,
            FechaFin = ahoraUtc.AddDays(-1),
            ImagenFondoUrl = string.Empty,
            EstaActiva = false
        };
    }
}
