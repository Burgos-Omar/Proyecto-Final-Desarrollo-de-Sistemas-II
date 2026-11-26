using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Services;
using FerreteriaERP.Infrastructure.Data;
using FerreteriaERP.UI.Desktop.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public record EmpleadoGerenciaItem(
    int Posicion,
    string NombreCompleto,
    string Puesto,
    int CantidadVentas,
    decimal TotalVendido)
{
    public string PosicionTexto => $"#{Posicion}";

    public string CantidadVentasTexto => CantidadVentas == 1
        ? "1 venta"
        : $"{CantidadVentas} ventas";

    public string TotalVendidoTexto => $"Bs. {TotalVendido:N2}";
}

public enum TipoFiltroGerencia
{
    Semana,
    Mes,
    Anio,
    Personalizado
}

/// <summary>
/// ViewModel de Inteligencia Gerencial (PASO 28) exclusivo para el Dueño / SuperAdmin.
/// </summary>
public partial class GerenciaViewModel : ViewModelBase
{
    private static readonly CultureInfo CulturaEs = new("es-BO");

    private readonly IDbContextFactory<FerreteriaDbContext> _dbContextFactory;
    private readonly IGastoOperativoRepository _gastoRepository;
    private readonly ICotizacionRepository _cotizacionRepository;
    private readonly IPromocionRepository _promocionRepository;
    private readonly ISesionService _sesionService;

    private TipoFiltroGerencia _tipoFiltroActual = TipoFiltroGerencia.Mes;

    [ObservableProperty]
    private DateTime? _fechaInicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private DateTime? _fechaFin = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CapitalInvertidoTexto))]
    private decimal _capitalInvertidoInventario;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IngresosBrutosTexto))]
    private decimal _ingresosBrutosPeriodo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CostoMercaderiaVendidaTexto))]
    private decimal _costoMercaderiaVendida;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalGastosExtrasTexto))]
    private decimal _totalGastosExtras;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GananciaLiquidaNetaTexto))]
    [NotifyPropertyChangedFor(nameof(EsGananciaPositiva))]
    [NotifyPropertyChangedFor(nameof(EsGananciaNegativa))]
    private decimal _gananciaLiquidaNeta;

    [ObservableProperty]
    private int _totalCotizacionesPeriodo;

    [ObservableProperty]
    private int _totalVentasPeriodo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TasaConversionTexto))]
    private decimal _tasaConversionPorcentaje;

    [ObservableProperty]
    private ISeries[] _seriesTendenciaMensual = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _ejeXMeses = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _seriesFlujoClientes = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _ejeXFlujo = Array.Empty<Axis>();

    [ObservableProperty]
    private string _subtituloFlujoClientes = "Ventas cerradas vs. proformas emitidas por día de la semana";

    [ObservableProperty]
    private string _nuevoGastoCategoria = "Alquiler";

    [ObservableProperty]
    private string _nuevoGastoDescripcion = string.Empty;

    [ObservableProperty]
    private string _nuevoGastoMonto = string.Empty;

    // Métricas reales de Rendimiento de Promociones (PASO 29)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PorcentajeVentasPromoTexto))]
    private decimal _porcentajeVentasConPromocion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalVentasPromoTexto))]
    private decimal _totalVentasPromocionales;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalDescontadoPromoTexto))]
    private decimal _totalDescontadoPromociones;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ControlesHabilitados))]
    private bool _estaCargando;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarError))]
    private string? _mensajeError;

    public GerenciaViewModel(
        IDbContextFactory<FerreteriaDbContext> dbContextFactory,
        IGastoOperativoRepository gastoRepository,
        ICotizacionRepository cotizacionRepository,
        IPromocionRepository promocionRepository,
        ISesionService sesionService)
    {
        _dbContextFactory = dbContextFactory;
        _gastoRepository = gastoRepository;
        _cotizacionRepository = cotizacionRepository;
        _promocionRepository = promocionRepository;
        _sesionService = sesionService;

        _ = EjecutarCargaRangoAsync(TipoFiltroGerencia.Mes);
    }

    public IReadOnlyList<string> CategoriasGasto { get; } = new[]
    {
        "Alquiler",
        "Salarios",
        "Servicios Básicos (Luz/Agua)",
        "Impuestos",
        "Otros"
    };

    public ObservableCollection<EmpleadoGerenciaItem> TopEmpleados { get; } = new();

    public ObservableCollection<GastoOperativo> GastosRecientes { get; } = new();

    public ObservableCollection<RendimientoCampanaItem> TopCampanasPromocionales { get; } = new();

    public bool ControlesHabilitados => !EstaCargando;

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeError);

    public string CapitalInvertidoTexto => $"Bs. {CapitalInvertidoInventario:N2}";

    public string IngresosBrutosTexto => $"Bs. {IngresosBrutosPeriodo:N2}";

    public string CostoMercaderiaVendidaTexto => $"Costo mercadería (CMV): Bs. {CostoMercaderiaVendida:N2}";

    public string TotalGastosExtrasTexto => $"Bs. {TotalGastosExtras:N2}";

    public string GananciaLiquidaNetaTexto => $"Bs. {GananciaLiquidaNeta:N2}";

    public bool EsGananciaPositiva => GananciaLiquidaNeta >= 0m;

    public bool EsGananciaNegativa => GananciaLiquidaNeta < 0m;

    public string TasaConversionTexto =>
        TotalCotizacionesPeriodo == 0
            ? $"Ventas vs Proformas: {TotalVentasPeriodo} / 0"
            : (TotalVentasPeriodo > TotalCotizacionesPeriodo
                ? $"Ventas vs Proformas: {TotalVentasPeriodo} / {TotalCotizacionesPeriodo}"
                : $"{TasaConversionPorcentaje:N1}% Conversión ({TotalVentasPeriodo} / {TotalCotizacionesPeriodo})");

    public bool SinTopEmpleados => !EstaCargando && TopEmpleados.Count == 0;

    public bool SinGastosRecientes => !EstaCargando && GastosRecientes.Count == 0;

    public bool SinTopCampanas => !EstaCargando && TopCampanasPromocionales.Count == 0;

    public string PorcentajeVentasPromoTexto => $"{PorcentajeVentasConPromocion:N1}% Adopción";

    public string TotalVentasPromoTexto => $"Bs. {TotalVentasPromocionales:N2}";

    public string TotalDescontadoPromoTexto => $"Bs. {TotalDescontadoPromociones:N2}";

    [RelayCommand]
    private async Task FiltrarSemanaAsync()
    {
        var hoy = DateTime.Today;
        var diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
        FechaInicio = hoy.AddDays(-diff);
        FechaFin = hoy;
        await EjecutarCargaRangoAsync(TipoFiltroGerencia.Semana);
    }

    [RelayCommand]
    private async Task FiltrarMesAsync()
    {
        var hoy = DateTime.Today;
        FechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
        FechaFin = hoy;
        await EjecutarCargaRangoAsync(TipoFiltroGerencia.Mes);
    }

    [RelayCommand]
    private async Task FiltrarAnioAsync()
    {
        var hoy = DateTime.Today;
        FechaInicio = new DateTime(hoy.Year, 1, 1);
        FechaFin = hoy;
        await EjecutarCargaRangoAsync(TipoFiltroGerencia.Anio);
    }

    [RelayCommand]
    public async Task AplicarRangoAsync()
    {
        await EjecutarCargaRangoAsync(TipoFiltroGerencia.Personalizado);
    }

    private async Task EjecutarCargaRangoAsync(TipoFiltroGerencia tipoFiltro)
    {
        if (EstaCargando)
            return;

        _tipoFiltroActual = tipoFiltro;
        MensajeError = null;
        EstaCargando = true;

        try
        {
            var inicioLocal = (FechaInicio ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var finLocal = (FechaFin ?? DateTime.Today).Date;
            if (finLocal < inicioLocal)
            {
                (inicioLocal, finLocal) = (finLocal, inicioLocal);
                FechaInicio = inicioLocal;
                FechaFin = finLocal;
            }

            var desdeUtc = DateTime.SpecifyKind(inicioLocal, DateTimeKind.Local).ToUniversalTime();
            var hastaUtc = DateTime.SpecifyKind(finLocal.AddDays(1), DateTimeKind.Local).ToUniversalTime();

            await using var db = await _dbContextFactory.CreateDbContextAsync();

            // 1. Capital Invertido en Inventario: Suma de (StockSucursal.CantidadActual * Producto.PrecioCompra)
            var stocksConCosto = await db.StockSucursales
                .AsNoTracking()
                .Include(s => s.Producto)
                .ToListAsync();

            var capitalTotal = stocksConCosto
                .Where(s => s.CantidadActual > 0 && s.Producto is not null)
                .Sum(s => s.CantidadActual * s.Producto.PrecioCompra);

            // 2. Ingresos Brutos y Costo de Mercadería Vendida (CMV) en el rango seleccionado
            var ventasPeriodo = await db.Ventas
                .AsNoTracking()
                .Include(v => v.Empleado)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(v => v.FechaHora >= desdeUtc && v.FechaHora < hastaUtc)
                .ToListAsync();

            var ingresosBrutos = ventasPeriodo.Sum(v => v.Total);

            var costoMercaderia = ventasPeriodo
                .SelectMany(v => v.Detalles)
                .Sum(d => d.Cantidad * (d.Producto?.PrecioCompra ?? 0m));

            // 3. Gastos Operativos / Extras del periodo
            var gastosPeriodo = await _gastoRepository.ObtenerPorRangoAsync(desdeUtc, hastaUtc);
            var totalGastos = gastosPeriodo.Sum(g => g.Monto);

            // 4. Ganancia Líquida Neta exacta
            var gananciaLiquida = ingresosBrutos - costoMercaderia - totalGastos;

            // 5. Embudo de Conversión (Proformas vs Ventas en el periodo)
            var todasCotizaciones = await _cotizacionRepository.GetAllAsync();
            var cotizacionesPeriodo = todasCotizaciones
                .Where(c => c.FechaEmision >= desdeUtc && c.FechaEmision < hastaUtc)
                .ToList();

            var cantCotizaciones = cotizacionesPeriodo.Count;
            var cantVentas = ventasPeriodo.Count;
            var tasaConversion = cantCotizaciones > 0
                ? Math.Min(100m, Math.Round((decimal)cantVentas / cantCotizaciones * 100m, 1))
                : 0m;

            // 6. Gráfico 1: Evolución Mensual de Ingresos Brutos (últimos 6 meses)
            var mesActualInicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var haceSeisMesesLocal = mesActualInicio.AddMonths(-5);
            var haceSeisMesesUtc = DateTime.SpecifyKind(haceSeisMesesLocal, DateTimeKind.Local).ToUniversalTime();

            var ventasUltimosMeses = await db.Ventas
                .AsNoTracking()
                .Where(v => v.FechaHora >= haceSeisMesesUtc)
                .Select(v => new { v.FechaHora, v.Total })
                .ToListAsync();

            var etiquetasMeses = new List<string>();
            var valoresMensuales = new List<decimal>();

            for (var i = 0; i < 6; i++)
            {
                var mesIter = haceSeisMesesLocal.AddMonths(i);
                var inicioMesIterUtc = DateTime.SpecifyKind(mesIter, DateTimeKind.Local).ToUniversalTime();
                var finMesIterUtc = DateTime.SpecifyKind(mesIter.AddMonths(1), DateTimeKind.Local).ToUniversalTime();

                var sumaMes = ventasUltimosMeses
                    .Where(v => v.FechaHora >= inicioMesIterUtc && v.FechaHora < finMesIterUtc)
                    .Sum(v => v.Total);

                var nombreCorto = CulturaEs.DateTimeFormat.GetAbbreviatedMonthName(mesIter.Month);
                etiquetasMeses.Add($"{nombreCorto.TrimEnd('.')} {mesIter:yy}");
                valoresMensuales.Add(sumaMes);
            }

            SeriesTendenciaMensual = new ISeries[]
            {
                new LineSeries<decimal>
                {
                    Name = "Ingresos Brutos (Bs)",
                    Values = valoresMensuales,
                    Stroke = new SolidColorPaint(SKColor.Parse("#10B981"), 3),
                    Fill = new SolidColorPaint(SKColor.Parse("#2210B981")),
                    GeometrySize = 9,
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#059669"), 3),
                    GeometryFill = new SolidColorPaint(SKColors.White)
                }
            };

            EjeXMeses = new[]
            {
                new Axis
                {
                    Labels = etiquetasMeses
                }
            };

            // 7. Gráfico 2: Flujo de Clientes y Conversión con Granularidad Temporal Dinámica
            CargarFlujoClientes(ventasPeriodo, cotizacionesPeriodo, inicioLocal, finLocal);

            // 8. Top Trabajadores del Periodo
            var ranking = ventasPeriodo
                .Where(v => v.Empleado is not null)
                .GroupBy(v => v.IdEmpleado!.Value)
                .Select(g =>
                {
                    var emp = g.First().Empleado!;
                    return new
                    {
                        emp.NombreCompleto,
                        emp.Puesto,
                        Cantidad = g.Count(),
                        Total = g.Sum(x => x.Total)
                    };
                })
                .OrderByDescending(x => x.Total)
                .ThenByDescending(x => x.Cantidad)
                .Take(6)
                .Select((x, idx) => new EmpleadoGerenciaItem(
                    Posicion: idx + 1,
                    NombreCompleto: x.NombreCompleto,
                    Puesto: x.Puesto,
                    CantidadVentas: x.Cantidad,
                    TotalVendido: x.Total))
                .ToList();

            // 9. Rendimiento Real de Promociones en el Periodo (PASO 29)
            var (porcentajePromo, totalVentasPromo, totalDescontadoPromo, topCampanas) =
                await _promocionRepository.CalcularRendimientoPeriodoAsync(desdeUtc, hastaUtc);

            // Actualizar estado observable en UI
            CapitalInvertidoInventario = capitalTotal;
            IngresosBrutosPeriodo = ingresosBrutos;
            CostoMercaderiaVendida = costoMercaderia;
            TotalGastosExtras = totalGastos;
            GananciaLiquidaNeta = gananciaLiquida;

            TotalCotizacionesPeriodo = cantCotizaciones;
            TotalVentasPeriodo = cantVentas;
            TasaConversionPorcentaje = tasaConversion;
            OnPropertyChanged(nameof(TasaConversionTexto));

            PorcentajeVentasConPromocion = porcentajePromo;
            TotalVentasPromocionales = totalVentasPromo;
            TotalDescontadoPromociones = totalDescontadoPromo;

            TopEmpleados.Clear();
            foreach (var item in ranking)
            {
                TopEmpleados.Add(item);
            }

            GastosRecientes.Clear();
            foreach (var gasto in gastosPeriodo.Take(10))
            {
                GastosRecientes.Add(gasto);
            }

            TopCampanasPromocionales.Clear();
            foreach (var camp in topCampanas)
            {
                TopCampanasPromocionales.Add(camp);
            }

            OnPropertyChanged(nameof(SinTopEmpleados));
            OnPropertyChanged(nameof(SinGastosRecientes));
            OnPropertyChanged(nameof(SinTopCampanas));
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al calcular métricas gerenciales: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void CargarFlujoClientes(
        List<Venta> ventasPeriodo,
        List<Cotizacion> cotizacionesPeriodo,
        DateTime inicioLocal,
        DateTime finLocal)
    {
        var diasRango = Math.Max(1, (finLocal.Date - inicioLocal.Date).Days + 1);

        string[] etiquetasEjeX;
        int[] valoresVentas;
        int[] valoresCotizaciones;

        // Caso 1: Semana (o rango personalizado <= 7 días)
        if (_tipoFiltroActual == TipoFiltroGerencia.Semana ||
            (_tipoFiltroActual == TipoFiltroGerencia.Personalizado && diasRango <= 7))
        {
            SubtituloFlujoClientes = "Ventas cerradas vs. proformas emitidas por día de la semana";

            var diasOrden = new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            };

            etiquetasEjeX = new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };

            valoresVentas = diasOrden
                .Select(d => ventasPeriodo.Count(v => v.FechaHora.ToLocalTime().DayOfWeek == d))
                .ToArray();

            valoresCotizaciones = diasOrden
                .Select(d => cotizacionesPeriodo.Count(c => c.FechaEmision.ToLocalTime().DayOfWeek == d))
                .ToArray();
        }
        // Caso 2: Mes (o rango personalizado entre 8 y 31 días)
        else if (_tipoFiltroActual == TipoFiltroGerencia.Mes ||
                 (_tipoFiltroActual == TipoFiltroGerencia.Personalizado && diasRango <= 31))
        {
            SubtituloFlujoClientes = "Ventas cerradas vs. proformas emitidas por semana del mes";

            var diasEnMes = DateTime.DaysInMonth(inicioLocal.Year, inicioLocal.Month);
            var cantidadSemanas = diasEnMes > 28 ? 5 : 4;

            etiquetasEjeX = cantidadSemanas == 5
                ? new[] { "Sem 1", "Sem 2", "Sem 3", "Sem 4", "Sem 5" }
                : new[] { "Sem 1", "Sem 2", "Sem 3", "Sem 4" };

            valoresVentas = new int[cantidadSemanas];
            valoresCotizaciones = new int[cantidadSemanas];

            foreach (var v in ventasPeriodo)
            {
                var dia = v.FechaHora.ToLocalTime().Day;
                var idxSemana = Math.Clamp((dia - 1) / 7, 0, cantidadSemanas - 1);
                valoresVentas[idxSemana]++;
            }

            foreach (var c in cotizacionesPeriodo)
            {
                var dia = c.FechaEmision.ToLocalTime().Day;
                var idxSemana = Math.Clamp((dia - 1) / 7, 0, cantidadSemanas - 1);
                valoresCotizaciones[idxSemana]++;
            }
        }
        // Caso 3: Año (o rango personalizado > 31 días)
        else
        {
            SubtituloFlujoClientes = "Ventas cerradas vs. proformas emitidas por mes del año";

            etiquetasEjeX = new[]
            {
                "Ene", "Feb", "Mar", "Abr", "May", "Jun",
                "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"
            };

            valoresVentas = new int[12];
            valoresCotizaciones = new int[12];

            foreach (var v in ventasPeriodo)
            {
                var idxMes = Math.Clamp(v.FechaHora.ToLocalTime().Month - 1, 0, 11);
                valoresVentas[idxMes]++;
            }

            foreach (var c in cotizacionesPeriodo)
            {
                var idxMes = Math.Clamp(c.FechaEmision.ToLocalTime().Month - 1, 0, 11);
                valoresCotizaciones[idxMes]++;
            }
        }

        SeriesFlujoClientes = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Name = "Ventas Cerradas",
                Values = valoresVentas,
                Fill = new SolidColorPaint(SKColor.Parse("#10B981"))
            },
            new ColumnSeries<int>
            {
                Name = "Proformas Emitidas",
                Values = valoresCotizaciones,
                Fill = new SolidColorPaint(SKColor.Parse("#0284C7"))
            }
        };

        EjeXFlujo = new[]
        {
            new Axis
            {
                Labels = etiquetasEjeX
            }
        };
    }

    [RelayCommand]
    private async Task RegistrarGastoAsync()
    {
        MensajeError = null;

        var categoria = string.IsNullOrWhiteSpace(NuevoGastoCategoria) ? "Otros" : NuevoGastoCategoria.Trim();
        var descripcion = string.IsNullOrWhiteSpace(NuevoGastoDescripcion)
            ? categoria
            : NuevoGastoDescripcion.Trim();

        var montoLimpio = (NuevoGastoMonto ?? string.Empty).Trim().Replace(',', '.');
        if (!decimal.TryParse(montoLimpio, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto) || monto <= 0m)
        {
            MostrarToast("Ingrese un monto de gasto válido mayor a Bs 0.00", NotificationType.Error);
            return;
        }

        try
        {
            var idSucursal = SesionActual.IdSucursal > 0
                ? SesionActual.IdSucursal
                : (_sesionService.EstaAutenticado ? _sesionService.IdSucursalActual : 1);

            var nuevoGasto = new GastoOperativo
            {
                Fecha = DateTime.UtcNow,
                Categoria = categoria,
                Descripcion = descripcion,
                Monto = monto,
                IdSucursal = idSucursal
            };

            await _gastoRepository.AgregarGastoAsync(nuevoGasto);

            NuevoGastoDescripcion = string.Empty;
            NuevoGastoMonto = string.Empty;

            // Si la FechaFin del filtro es menor a hoy, la extendemos a hoy para incluir el gasto recién creado
            if (!FechaFin.HasValue || FechaFin.Value.Date < DateTime.Today)
            {
                FechaFin = DateTime.Today;
            }

            await AplicarRangoAsync();
            MostrarToast($"Gasto de Bs {monto:N2} ({categoria}) registrado", NotificationType.Success);
        }
        catch (Exception ex)
        {
            MostrarToast("Error al registrar gasto: " + ex.Message, NotificationType.Error);
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
                "Inteligencia Gerencial",
                mensaje,
                tipo,
                TimeSpan.FromSeconds(4)));
        });
    }
}
