using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FerreteriaERP.Core.Constants;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.UI.Desktop.ViewModels;

public partial class CreditosViewModel : ViewModelBase
{
    private readonly IDbContextFactory<FerreteriaDbContext> _contextFactory;
    private readonly IWhatsAppService _whatsAppService;

    private List<Credito> _todosLosCreditos = new();

    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsFiltroPendientes))]
    [NotifyPropertyChangedFor(nameof(NoEsFiltroPendientes))]
    [NotifyPropertyChangedFor(nameof(EsFiltroPagados))]
    [NotifyPropertyChangedFor(nameof(NoEsFiltroPagados))]
    private string _estadoFiltroSeleccionado = EstadosCredito.Pendiente;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneCreditoSeleccionado))]
    [NotifyPropertyChangedFor(nameof(SinCreditoSeleccionado))]
    [NotifyPropertyChangedFor(nameof(PuedeRegistrarAbono))]
    [NotifyPropertyChangedFor(nameof(PuedeNotificarWhatsApp))]
    [NotifyPropertyChangedFor(nameof(NombreClienteSeleccionado))]
    [NotifyPropertyChangedFor(nameof(CiClienteSeleccionado))]
    [NotifyPropertyChangedFor(nameof(MontoTotalSeleccionadoTexto))]
    [NotifyPropertyChangedFor(nameof(SaldoPendienteSeleccionadoTexto))]
    [NotifyPropertyChangedFor(nameof(VencimientoSeleccionadoTexto))]
    [NotifyPropertyChangedFor(nameof(TextoSaldarDeudaCompleta))]
    private Credito? _creditoSeleccionado;

    [ObservableProperty]
    private string _montoAbonoTexto = string.Empty;

    [ObservableProperty]
    private string _celularClienteInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ControlesHabilitados))]
    [NotifyPropertyChangedFor(nameof(PuedeRegistrarAbono))]
    [NotifyPropertyChangedFor(nameof(PuedeNotificarWhatsApp))]
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
    private decimal _totalPorCobrar;

    [ObservableProperty]
    private int _creditosVencidosCount;

    [ObservableProperty]
    private int _creditosPendientesCount;

    public CreditosViewModel(
        IDbContextFactory<FerreteriaDbContext> contextFactory,
        IWhatsAppService whatsAppService)
    {
        _contextFactory = contextFactory;
        _whatsAppService = whatsAppService;

        _ = CargarCreditosAsync();
    }

    /// <summary>
    /// Colección reactiva de créditos filtrados junto con los datos de su Cliente.
    /// </summary>
    public ObservableCollection<Credito> ListaCreditos { get; } = new();

    /// <summary>
    /// Historial de abonos del crédito actualmente seleccionado.
    /// </summary>
    public ObservableCollection<AbonoCredito> AbonosDelCreditoSeleccionado { get; } = new();

    /// <summary>
    /// Opciones para el ComboBox de estado ("Pendiente" por defecto o "Pagado").
    /// </summary>
    public IReadOnlyList<string> OpcionesEstadoFiltro { get; } = new[]
    {
        EstadosCredito.Pendiente,
        EstadosCredito.Pagado
    };

    public bool SinResultados => ListaCreditos.Count == 0;

    public bool ControlesHabilitados => !EstaOcupado;

    public bool EsFiltroPendientes =>
        string.Equals(EstadoFiltroSeleccionado, EstadosCredito.Pendiente, StringComparison.OrdinalIgnoreCase);

    public bool NoEsFiltroPendientes => !EsFiltroPendientes;

    public bool EsFiltroPagados =>
        string.Equals(EstadoFiltroSeleccionado, EstadosCredito.Pagado, StringComparison.OrdinalIgnoreCase);

    public bool NoEsFiltroPagados => !EsFiltroPagados;

    public bool TieneCreditoSeleccionado => CreditoSeleccionado is not null;

    public bool SinCreditoSeleccionado => CreditoSeleccionado is null;

    public bool PuedeRegistrarAbono =>
        !EstaOcupado &&
        CreditoSeleccionado is not null &&
        CreditoSeleccionado.EsPendiente &&
        CreditoSeleccionado.SaldoPendiente > 0m;

    public bool PuedeNotificarWhatsApp =>
        !EstaOcupado &&
        CreditoSeleccionado is not null &&
        CreditoSeleccionado.SaldoPendiente > 0m;

    /// <summary>
    /// Texto dinámico para el botón de saldo completo: "Saldar deuda completa (Bs X.XX)".
    /// </summary>
    public string TextoSaldarDeudaCompleta =>
        CreditoSeleccionado is not null
            ? $"Saldar deuda completa (Bs {CreditoSeleccionado.SaldoPendiente:N2})"
            : "Saldar deuda completa";

    public string NombreClienteSeleccionado =>
        CreditoSeleccionado?.Cliente?.NombreCompleto ?? "Seleccione un crédito";

    public string CiClienteSeleccionado =>
        string.IsNullOrWhiteSpace(CreditoSeleccionado?.Cliente?.Ci)
            ? "Sin C.I."
            : $"C.I.: {CreditoSeleccionado.Cliente.Ci}";

    public string MontoTotalSeleccionadoTexto =>
        CreditoSeleccionado is null ? "Bs 0.00" : $"Bs {CreditoSeleccionado.MontoTotalDeuda:N2}";

    public string SaldoPendienteSeleccionadoTexto =>
        CreditoSeleccionado is null ? "Bs 0.00" : $"Bs {CreditoSeleccionado.SaldoPendiente:N2}";

    public string VencimientoSeleccionadoTexto =>
        CreditoSeleccionado?.FechaVencimientoTexto ?? "—";

    public bool MostrarError => !string.IsNullOrWhiteSpace(MensajeEstado) && !EsMensajeExito;

    public bool MostrarExito => !string.IsNullOrWhiteSpace(MensajeEstado) && EsMensajeExito;

    partial void OnTextoBusquedaChanged(string value)
    {
        AplicarFiltroLocal();
    }

    partial void OnEstadoFiltroSeleccionadoChanged(string value)
    {
        LimpiarMensaje();
        AplicarFiltroLocal();
    }

    partial void OnCreditoSeleccionadoChanged(Credito? value)
    {
        AbonosDelCreditoSeleccionado.Clear();

        if (value is null)
        {
            MontoAbonoTexto = string.Empty;
            CelularClienteInput = string.Empty;
            return;
        }

        CelularClienteInput = value.Cliente?.CelularWhatsapp ?? string.Empty;
        // PASO 18: MontoAbono siempre inicia vacío al seleccionar un crédito.
        // El cajero ingresa el monto manualmente o usa "Saldar deuda completa".
        MontoAbonoTexto = string.Empty;

        if (value.Abonos is not null)
        {
            foreach (var abono in value.Abonos.OrderByDescending(a => a.FechaPago))
            {
                AbonosDelCreditoSeleccionado.Add(abono);
            }
        }
    }

    [RelayCommand]
    private void FiltrarPendientes()
    {
        EstadoFiltroSeleccionado = EstadosCredito.Pendiente;
    }

    [RelayCommand]
    private void FiltrarPagados()
    {
        EstadoFiltroSeleccionado = EstadosCredito.Pagado;
    }

    [RelayCommand]
    private void UsarSaldoTotalComoAbono()
    {
        if (CreditoSeleccionado is null)
            return;

        MontoAbonoTexto = CreditoSeleccionado.SaldoPendiente.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// PASO 18: Pone el saldo pendiente completo en el campo de abono (alias semántico más claro para la UI).
    /// </summary>
    [RelayCommand]
    private void SaldarDeudaCompleta()
    {
        if (CreditoSeleccionado is null)
            return;

        MontoAbonoTexto = CreditoSeleccionado.SaldoPendiente.ToString("0.00", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private async Task RecargarAsync()
    {
        LimpiarMensaje();
        await CargarCreditosAsync();
    }

    private async Task CargarCreditosAsync(int? idCreditoReseleccionar = null)
    {
        EstaOcupado = true;
        try
        {
            var idPrevio = idCreditoReseleccionar ?? CreditoSeleccionado?.Id;

            await using var db = await _contextFactory.CreateDbContextAsync();

            _todosLosCreditos = await db.Creditos
                .AsNoTracking()
                .Include(c => c.Cliente)
                .Include(c => c.Abonos)
                .OrderBy(c => c.FechaVencimiento)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            var pendientes = _todosLosCreditos.Where(c => c.EsPendiente).ToList();
            CreditosPendientesCount = pendientes.Count;
            CreditosVencidosCount = pendientes.Count(c => c.EsVencido);
            TotalPorCobrar = pendientes.Sum(c => c.SaldoPendiente);

            AplicarFiltroLocal(idPrevio);
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"Error al cargar la cartera de créditos: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AplicarFiltroLocal(int? idCreditoPreferido = null)
    {
        IEnumerable<Credito> consulta = _todosLosCreditos.Where(c =>
            string.Equals(c.Estado, EstadoFiltroSeleccionado, StringComparison.OrdinalIgnoreCase));

        var termino = TextoBusqueda?.Trim();
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(c =>
                (c.Cliente?.NombreCompleto?.Contains(termino, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Cliente?.Ci?.Contains(termino, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Cliente?.CelularWhatsapp?.Contains(termino, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        ListaCreditos.Clear();
        foreach (var credito in consulta)
        {
            ListaCreditos.Add(credito);
        }

        OnPropertyChanged(nameof(SinResultados));

        if (idCreditoPreferido is int idBuscado)
        {
            CreditoSeleccionado = ListaCreditos.FirstOrDefault(c => c.Id == idBuscado)
                                  ?? ListaCreditos.FirstOrDefault();
        }
        else if (CreditoSeleccionado is null || !ListaCreditos.Contains(CreditoSeleccionado))
        {
            CreditoSeleccionado = ListaCreditos.FirstOrDefault();
        }
    }

    /// <summary>
    /// Registra un abono en una transacción atómica usando <see cref="IDbContextFactory{TContext}"/>:
    /// inserta <see cref="AbonoCredito"/>, descuenta <c>SaldoPendiente</c> y cambia el estado a "Pagado"
    /// cuando el saldo llega a 0.
    /// </summary>
    [RelayCommand]
    private async Task RegistrarAbonoAsync()
    {
        LimpiarMensaje();

        if (CreditoSeleccionado is null)
        {
            MostrarErrorEstado("Seleccione un crédito de la tabla para registrar un abono.");
            return;
        }

        if (!CreditoSeleccionado.EsPendiente || CreditoSeleccionado.SaldoPendiente <= 0m)
        {
            MostrarErrorEstado("Este crédito ya se encuentra completamente pagado.");
            return;
        }

        if (!IntentarParsearDecimal(MontoAbonoTexto, out var montoAbono) || montoAbono <= 0m)
        {
            MostrarErrorEstado("Ingrese un monto de abono válido mayor a Bs 0.00.");
            return;
        }

        if (montoAbono > CreditoSeleccionado.SaldoPendiente)
        {
            MostrarErrorEstado(
                $"El monto del abono (Bs {montoAbono:N2}) no puede superar el saldo pendiente (Bs {CreditoSeleccionado.SaldoPendiente:N2}).");
            return;
        }

        var idCredito = CreditoSeleccionado.Id;
        var nombreCliente = CreditoSeleccionado.Cliente?.NombreCompleto ?? "Cliente";

        EstaOcupado = true;
        try
        {
            decimal nuevoSaldo;
            bool quedoPagado;

            await using (var db = await _contextFactory.CreateDbContextAsync())
            await using (var transaccion = await db.Database.BeginTransactionAsync())
            {
                var creditoDb = await db.Creditos
                    .Include(c => c.Cliente)
                    .FirstOrDefaultAsync(c => c.Id == idCredito);

                if (creditoDb is null)
                    throw new InvalidOperationException("El crédito seleccionado ya no existe en la base de datos.");

                if (!string.Equals(creditoDb.Estado, EstadosCredito.Pendiente, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("El crédito ya fue marcado como Pagado.");

                if (montoAbono > creditoDb.SaldoPendiente)
                    throw new InvalidOperationException("El abono supera el saldo pendiente actual en la base de datos.");

                // Si el usuario actualizó el número de WhatsApp en el panel lateral, lo guardamos también
                var celularLimpio = CelularClienteInput?.Trim();
                if (creditoDb.Cliente is not null && !string.IsNullOrWhiteSpace(celularLimpio))
                {
                    creditoDb.Cliente.CelularWhatsapp = celularLimpio;
                }

                // 1. Crear el registro en AbonoCredito
                var nuevoAbono = new AbonoCredito
                {
                    IdCredito = creditoDb.Id,
                    MontoPagado = montoAbono,
                    FechaPago = DateTime.UtcNow
                };
                db.AbonosCredito.Add(nuevoAbono);

                // 2. Restar el monto al SaldoPendiente del Crédito
                creditoDb.SaldoPendiente -= montoAbono;

                // 3. Si el saldo llega a 0, cambiar el estado del crédito a "Pagado"
                if (creditoDb.SaldoPendiente <= 0m)
                {
                    creditoDb.SaldoPendiente = 0m;
                    creditoDb.Estado = EstadosCredito.Pagado;
                    quedoPagado = true;
                }
                else
                {
                    quedoPagado = false;
                }

                nuevoSaldo = creditoDb.SaldoPendiente;

                await db.SaveChangesAsync();
                await transaccion.CommitAsync();
            }

            await CargarCreditosAsync(idCredito);

            if (quedoPagado)
            {
                MostrarExitoEstado(
                    $"¡Abono de Bs {montoAbono:N2} registrado! El crédito de '{nombreCliente}' ha quedado totalmente PAGADO.");
            }
            else
            {
                MostrarExitoEstado(
                    $"Abono de Bs {montoAbono:N2} registrado para '{nombreCliente}'. Nuevo saldo pendiente: Bs {nuevoSaldo:N2}.");
            }
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo registrar el abono: {ex.Message}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    /// <summary>
    /// Recibe el Crédito seleccionado (o el actual) y ejecuta <see cref="IWhatsAppService.EnviarRecordatorioPago"/>.
    /// </summary>
    [RelayCommand]
    private async Task EnviarRecordatorioAsync(Credito? creditoParametro)
    {
        LimpiarMensaje();

        var credito = creditoParametro ?? CreditoSeleccionado;
        if (credito is null)
        {
            MostrarErrorEstado("Seleccione un crédito para enviar el recordatorio por WhatsApp.");
            return;
        }

        var celular = !string.IsNullOrWhiteSpace(CelularClienteInput)
            ? CelularClienteInput.Trim()
            : credito.Cliente?.CelularWhatsapp?.Trim();

        if (string.IsNullOrWhiteSpace(celular))
        {
            MostrarErrorEstado(
                "El cliente no tiene un número de celular registrado. Ingréselo en el campo 'Celular WhatsApp (con cód. país)' del panel derecho.");
            return;
        }

        try
        {
            // Si el cliente no tenía ese celular guardado o fue modificado en el panel, lo persistimos en BD
            if (credito.Cliente is not null &&
                !string.Equals(credito.Cliente.CelularWhatsapp, celular, StringComparison.Ordinal))
            {
                await using var db = await _contextFactory.CreateDbContextAsync();
                await db.Clientes
                    .Where(c => c.Id == credito.IdCliente)
                    .ExecuteUpdateAsync(c => c.SetProperty(x => x.CelularWhatsapp, celular));

                credito.Cliente.CelularWhatsapp = celular;
                await CargarCreditosAsync(credito.Id);
            }

            var nombreCliente = credito.Cliente?.NombreCompleto ?? "Cliente";
            var fechaVencimiento = credito.FechaVencimiento ?? DateTime.UtcNow;

            _whatsAppService.EnviarRecordatorioPago(
                celular,
                nombreCliente,
                credito.SaldoPendiente,
                fechaVencimiento);

            MostrarExitoEstado(
                $"Se abrió WhatsApp con el recordatorio de cobro para '{nombreCliente}' ({celular}).");
        }
        catch (Exception ex)
        {
            MostrarErrorEstado($"No se pudo abrir WhatsApp: {ex.Message}");
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

