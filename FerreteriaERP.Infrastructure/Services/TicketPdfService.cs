using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Core.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FerreteriaERP.Infrastructure.Services;

/// <summary>
/// Generador de Recibos / Notas de Venta en PDF (formato media carta boliviano) utilizando QuestPDF.
/// </summary>
public class TicketPdfService
{
    private static readonly CultureInfo CulturaBolivia = new("es-BO");
    private readonly IConfiguracionRepository? _configuracionRepository;

    public TicketPdfService(IConfiguracionRepository? configuracionRepository = null)
    {
        _configuracionRepository = configuracionRepository;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private sealed record DatosNegocioPdf(
        string NombreNegocio,
        string? NitNegocio,
        byte[]? LogoImagen,
        string LineaContacto,
        string MensajePieRecibo);

    private DatosNegocioPdf ObtenerDatosFiscales(string nombreFallback = "FERRETERÍA ERP")
    {
        const string contactoFallback = "Tarija - Bolivia";
        const string pieFallback = "¡Gracias por su preferencia!";

        if (_configuracionRepository is null)
            return new DatosNegocioPdf(nombreFallback, null, null, contactoFallback, pieFallback);

        try
        {
            var config = _configuracionRepository.ObtenerConfiguracionAsync().GetAwaiter().GetResult();
            var nombre = string.IsNullOrWhiteSpace(config?.NombreNegocio)
                ? nombreFallback
                : config.NombreNegocio.Trim();
            var nit = string.IsNullOrWhiteSpace(config?.NitNegocio)
                ? null
                : config.NitNegocio.Trim();
            var logo = config?.LogoImagen is { Length: > 0 } ? config.LogoImagen : null;

            var direccion = string.IsNullOrWhiteSpace(config?.Direccion) ? null : config.Direccion.Trim();
            var telefono = string.IsNullOrWhiteSpace(config?.Telefono) ? null : config.Telefono.Trim();

            string lineaContacto = (direccion, telefono) switch
            {
                (not null, not null) => $"Dir: {direccion}  |  Tel: {telefono}",
                (not null, null) => $"Dir: {direccion}",
                (null, not null) => $"{contactoFallback}  |  Tel: {telefono}",
                _ => contactoFallback
            };

            var pie = string.IsNullOrWhiteSpace(config?.MensajePieRecibo)
                ? pieFallback
                : config.MensajePieRecibo.Trim();

            return new DatosNegocioPdf(nombre, nit, logo, lineaContacto, pie);
        }
        catch
        {
            return new DatosNegocioPdf(nombreFallback, null, null, contactoFallback, pieFallback);
        }
    }

    public void GenerarNotaDeVentaPdf(Venta venta, Empleado cajero, Cliente cliente, string rutaArchivo)
    {
        ArgumentNullException.ThrowIfNull(venta);
        if (string.IsNullOrWhiteSpace(rutaArchivo))
            throw new ArgumentException("Debe especificar una ruta válida para guardar el PDF.", nameof(rutaArchivo));

        var directorio = Path.GetDirectoryName(rutaArchivo);
        if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        var fechaLocal = venta.FechaHora.Kind == DateTimeKind.Utc
            ? venta.FechaHora.ToLocalTime()
            : venta.FechaHora;

        var nombreMes = CulturaBolivia.DateTimeFormat.GetMonthName(fechaLocal.Month);
        if (!string.IsNullOrEmpty(nombreMes))
            nombreMes = char.ToUpperInvariant(nombreMes[0]) + nombreMes.Substring(1);

        var fechaLiteral = $"Tarija, {fechaLocal:dd} de {nombreMes} de {fechaLocal:yyyy}";
        var nombreCliente = !string.IsNullOrWhiteSpace(cliente?.NombreCompleto)
            ? cliente.NombreCompleto.Trim()
            : "SIN NOMBRE";
        var nitCiCliente = !string.IsNullOrWhiteSpace(cliente?.Ci)
            ? cliente.Ci.Trim()
            : "0";
        var nombreVendedor = !string.IsNullOrWhiteSpace(cajero?.NombreCompleto)
            ? cajero.NombreCompleto.Trim()
            : "Mostrador";

        var numeroNota = venta.Id > 0 ? venta.Id.ToString("D4") : "0001";
        var montoLiteral = NumeroALetras.Convertir(venta.Total, "BOLIVIANOS");

        var rutaLogoOpcional = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
        var existeLogo = File.Exists(rutaLogoOpcional);
        var datosNegocio = ObtenerDatosFiscales("FERRETERÍA ERP");
        var nombreNegocio = datosNegocio.NombreNegocio;
        var nitNegocio = datosNegocio.NitNegocio;
        var logoImagen = datosNegocio.LogoImagen;
        var nitMostrar = string.IsNullOrWhiteSpace(nitNegocio) ? "4120895014" : nitNegocio;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(18, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#1E293B"));

                // =========================================================
                // 1. CABECERA (IZQUIERDA: EMPRESA / LOGO | DERECHA: NIT / N°)
                // =========================================================
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        // Cabecera Izquierda: Logo + Nombre Empresa + Dirección
                        row.RelativeItem(3).Row(leftRow =>
                        {
                            leftRow.ConstantItem(56).Height(50).Border(1).BorderColor("#CBD5E1")
                                .Background("#F8FAFC").AlignCenter().AlignMiddle().Element(logoBox =>
                                {
                                    if (logoImagen is { Length: > 0 })
                                    {
                                        logoBox.Image(logoImagen).FitArea();
                                    }
                                    else if (existeLogo)
                                    {
                                        logoBox.Image(rutaLogoOpcional).FitArea();
                                    }
                                    else
                                    {
                                        logoBox.Text("LOGO\nFERRETERÍA")
                                            .FontSize(7.5f)
                                            .Bold()
                                            .FontColor("#059669")
                                            .AlignCenter();
                                    }
                                });

                            leftRow.ConstantItem(10);

                            leftRow.RelativeItem().Column(empCol =>
                            {
                                empCol.Item().Text(nombreNegocio.ToUpperInvariant()).FontSize(15).Bold().FontColor("#0F172A");
                                empCol.Item().Text("Materiales de Construcción, Herramientas y Pinturas")
                                    .FontSize(8.5f).FontColor("#475569");
                                empCol.Item().Text(datosNegocio.LineaContacto).FontSize(8.5f).SemiBold().FontColor("#1E293B");
                            });
                        });

                        row.ConstantItem(12);

                        // Cabecera Derecha: Recuadro con NIT, NOTA DE VENTA N° 000X, ORIGINAL
                        row.ConstantItem(195).Border(1.2f).BorderColor(Colors.Black).Padding(8).Column(nitCol =>
                        {
                            nitCol.Item().AlignCenter().Text($"NIT: {nitMostrar}").FontSize(9.5f).Bold();
                            nitCol.Item().PaddingTop(2).AlignCenter()
                                .Text($"NOTA DE VENTA N° {numeroNota}")
                                .FontSize(11).Bold().FontColor("#B91C1C");
                            nitCol.Item().PaddingTop(2).AlignCenter()
                                .Text("ORIGINAL").FontSize(8.5f).SemiBold().FontColor("#334155");
                        });
                    });

                    headerCol.Item().PaddingTop(8).Border(1).BorderColor(Colors.Black).Padding(7).Column(cliCol =>
                    {
                        cliCol.Item().Row(r =>
                        {
                            r.RelativeItem(2).Text(txt =>
                            {
                                txt.Span("Lugar y Fecha: ").Bold();
                                txt.Span(fechaLiteral);
                            });

                            r.RelativeItem(1).AlignRight().Text(txt =>
                            {
                                txt.Span("NIT/CI: ").Bold();
                                txt.Span(nitCiCliente);
                            });
                        });

                        cliCol.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem(2).Text(txt =>
                            {
                                txt.Span("Señor(es): ").Bold();
                                txt.Span(nombreCliente);
                            });

                            r.RelativeItem(1).AlignRight().Text(txt =>
                            {
                                txt.Span("Pago / Vend.: ").Bold();
                                txt.Span($"{venta.MetodoPago} ({nombreVendedor})");
                            });
                        });
                    });
                });

                // =========================================================
                // 2. CUERPO: TABLA DE DETALLES DE LA VENTA
                // =========================================================
                page.Content().PaddingTop(8).Column(bodyCol =>
                {
                    bodyCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(52); // CANT.
                            columns.RelativeColumn();   // CONCEPTO
                            columns.ConstantColumn(88); // PRECIO UNITARIO
                            columns.ConstantColumn(88); // TOTAL
                        });

                        // Encabezado de tabla con bordes oscuros
                        table.Header(header =>
                        {
                            header.Cell().Element(CeldaEncabezado).AlignCenter().Text("CANT.");
                            header.Cell().Element(CeldaEncabezado).Text("CONCEPTO");
                            header.Cell().Element(CeldaEncabezado).AlignRight().Text("PRECIO UNIT.");
                            header.Cell().Element(CeldaEncabezado).AlignRight().Text("TOTAL");

                            static IContainer CeldaEncabezado(IContainer container)
                            {
                                return container
                                    .Border(1)
                                    .BorderColor(Colors.Black)
                                    .Background("#E2E8F0")
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(6)
                                    .DefaultTextStyle(x => x.Bold().FontSize(9));
                            }
                        });

                        var detalles = venta.Detalles?.ToList() ?? [];
                        foreach (var item in detalles)
                        {
                            var concepto = !string.IsNullOrWhiteSpace(item.Producto?.Nombre)
                                ? item.Producto.Nombre
                                : $"Producto #{item.IdProducto}";

                            table.Cell().Element(CeldaDetalle).AlignCenter().Text(item.Cantidad.ToString());
                            table.Cell().Element(CeldaDetalle).Column(col =>
                            {
                                col.Item().Text(concepto);
                                if (item.MontoDescuento > 0m)
                                {
                                    var etiquetaPromo = !string.IsNullOrWhiteSpace(item.NombrePromocionAplicada)
                                        ? item.NombrePromocionAplicada
                                        : "Promoción aplicada";
                                    col.Item().Text($"Desc. {etiquetaPromo}: -Bs. {item.MontoDescuento:N2}")
                                        .FontSize(7.8f)
                                        .Italic()
                                        .FontColor("#059669");
                                }
                            });
                            table.Cell().Element(CeldaDetalle).AlignRight().Text($"{item.PrecioUnitario:N2}");
                            table.Cell().Element(CeldaDetalle).AlignRight().Text($"{item.Subtotal:N2}");
                        }

                        static IContainer CeldaDetalle(IContainer container)
                        {
                            return container
                                .Border(0.75f)
                                .BorderColor(Colors.Black)
                                .PaddingVertical(4)
                                .PaddingHorizontal(6);
                        }
                    });

                    // =====================================================
                    // 3. TOTAL LITERAL Y NUMÉRICO
                    // =====================================================
                    bodyCol.Item().Border(1).BorderColor(Colors.Black).Padding(6).Row(totRow =>
                    {
                        totRow.RelativeItem().AlignMiddle().Text(txt =>
                        {
                            txt.Span("SON: ").Bold();
                            txt.Span(montoLiteral);
                        });

                        totRow.ConstantItem(176).AlignRight().AlignMiddle().Text(txt =>
                        {
                            txt.Span("TOTAL Bs.: ").FontSize(10.5f).Bold();
                            txt.Span($"{venta.Total:N2}").FontSize(11).Bold();
                        });
                    });
                });

                // =========================================================
                // 4. PIE DE PÁGINA: MENSAJE PERSONALIZADO Y LEYENDA LEGAL
                // =========================================================
                page.Footer().PaddingTop(4).Column(footerCol =>
                {
                    footerCol.Item().LineHorizontal(0.5f).LineColor("#94A3B8");
                    footerCol.Item().PaddingTop(3).AlignCenter().Text(datosNegocio.MensajePieRecibo)
                        .FontSize(8f)
                        .Italic()
                        .FontColor("#334155");
                    footerCol.Item().PaddingTop(2).AlignCenter().Text(
                            "\"La reproducción total o parcial y/o el uso no autorizado de esta Nota Fiscal, constituye un delito a ser sancionado conforme a Ley.\"")
                        .FontSize(7f)
                        .Italic()
                        .FontColor("#64748B");
                });
            });
        }).GeneratePdf(rutaArchivo);
    }

    public void GenerarReporteCierreCajaPdf(
        DateTime fecha,
        string nombreCajero,
        string sucursal,
        decimal totalEfectivo,
        decimal totalQR,
        decimal totalCreditos,
        string rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
            throw new ArgumentException("Debe especificar una ruta válida para guardar el PDF.", nameof(rutaArchivo));

        var directorio = Path.GetDirectoryName(rutaArchivo);
        if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        var fechaLocal = fecha.Kind == DateTimeKind.Utc ? fecha.ToLocalTime() : fecha;
        var horaEmision = DateTime.Now;
        var cajeroTexto = string.IsNullOrWhiteSpace(nombreCajero) ? "Cajero de Turno" : nombreCajero.Trim();
        var sucursalTexto = string.IsNullOrWhiteSpace(sucursal) ? "Sucursal Central" : sucursal.Trim();
        var totalGeneral = totalEfectivo + totalQR + totalCreditos;
        var totalLiteral = NumeroALetras.Convertir(totalGeneral, "BOLIVIANOS");
        var datosNegocio = ObtenerDatosFiscales("FERRETERÍA ERP");
        var nombreNegocio = datosNegocio.NombreNegocio;
        var nitNegocio = datosNegocio.NitNegocio;
        var logoImagen = datosNegocio.LogoImagen;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Portrait());
                page.Margin(22, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#1E293B"));

                // =========================================================
                // 1. CABECERA DEL REPORTE
                // =========================================================
                page.Header().Column(headerCol =>
                {
                    if (logoImagen is { Length: > 0 })
                    {
                        headerCol.Item().PaddingBottom(4).AlignCenter().MaxHeight(38).Image(logoImagen).FitArea();
                    }
                    headerCol.Item().AlignCenter().Text(nombreNegocio.ToUpperInvariant()).FontSize(11).Bold().FontColor("#059669");
                    if (!string.IsNullOrWhiteSpace(nitNegocio))
                    {
                        headerCol.Item().AlignCenter().Text($"NIT: {nitNegocio}").FontSize(8.5f).SemiBold().FontColor("#334155");
                    }
                    headerCol.Item().AlignCenter().Text(datosNegocio.LineaContacto).FontSize(8f).FontColor("#475569");
                    headerCol.Item().PaddingTop(2).AlignCenter()
                        .Text("REPORTE DE CIERRE DE CAJA")
                        .FontSize(15).Bold().FontColor("#0F172A");
                    headerCol.Item().PaddingTop(2).AlignCenter()
                        .Text("Arqueo de Turno y Resumen de Operaciones del Día")
                        .FontSize(8.5f).FontColor("#64748B");

                    headerCol.Item().PaddingTop(10).Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(10).Column(infoCol =>
                    {
                        infoCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text(txt =>
                            {
                                txt.Span("Fecha de Arqueo: ").Bold();
                                txt.Span($"{fechaLocal:dd/MM/yyyy}");
                            });
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Hora de Emisión: ").Bold();
                                txt.Span($"{horaEmision:HH:mm:ss}");
                            });
                        });

                        infoCol.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text(txt =>
                            {
                                txt.Span("Cajero Responsable: ").Bold();
                                txt.Span(cajeroTexto);
                            });
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Sucursal: ").Bold();
                                txt.Span(sucursalTexto);
                            });
                        });
                    });
                });

                // =========================================================
                // 2. CUERPO: TABLA RESUMEN DE TOTALES Y FIRMAS
                // =========================================================
                page.Content().PaddingTop(14).Column(bodyCol =>
                {
                    bodyCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CeldaEncabezado).Text("CONCEPTO / MODALIDAD");
                            header.Cell().Element(CeldaEncabezado).AlignRight().Text("IMPORTE (Bs.)");

                            static IContainer CeldaEncabezado(IContainer c) =>
                                c.Border(1)
                                 .BorderColor(Colors.Black)
                                 .Background("#E2E8F0")
                                 .PaddingVertical(6)
                                 .PaddingHorizontal(8)
                                 .DefaultTextStyle(x => x.Bold().FontSize(9.5f));
                        });

                        // Fila 1: Efectivo en Caja
                        table.Cell().Element(CeldaFila).Text("EFECTIVO EN CAJA:");
                        table.Cell().Element(CeldaFila).AlignRight().Text($"{totalEfectivo:N2} Bs.");

                        // Fila 2: Ingresos por QR / Transferencia
                        table.Cell().Element(CeldaFila).Text("INGRESOS POR QR/TRANSFERENCIA:");
                        table.Cell().Element(CeldaFila).AlignRight().Text($"{totalQR:N2} Bs.");

                        // Fila 3: Ventas al Crédito
                        table.Cell().Element(CeldaFila).Text("VENTAS AL CRÉDITO:");
                        table.Cell().Element(CeldaFila).AlignRight().Text($"{totalCreditos:N2} Bs.");

                        // Fila 4: Total del Día
                        table.Cell().Element(CeldaTotal).Text("TOTAL DEL DÍA:");
                        table.Cell().Element(CeldaTotal).AlignRight().Text($"{totalGeneral:N2} Bs.");

                        static IContainer CeldaFila(IContainer c) =>
                            c.Border(0.75f)
                             .BorderColor(Colors.Black)
                             .PaddingVertical(7)
                             .PaddingHorizontal(8);

                        static IContainer CeldaTotal(IContainer c) =>
                            c.Border(1.2f)
                             .BorderColor(Colors.Black)
                             .Background("#ECFDF5")
                             .PaddingVertical(8)
                             .PaddingHorizontal(8)
                             .DefaultTextStyle(x => x.Bold().FontSize(11).FontColor("#065F46"));
                    });

                    bodyCol.Item().PaddingTop(8).Border(1).BorderColor("#CBD5E1").Padding(7).Text(txt =>
                    {
                        txt.Span("SON: ").Bold().FontSize(8.5f);
                        txt.Span(totalLiteral).FontSize(8.5f);
                    });

                    // Espacio para firmas al final
                    bodyCol.Item().PaddingTop(65).Row(firmasRow =>
                    {
                        firmasRow.RelativeItem().Column(f1 =>
                        {
                            f1.Item().PaddingHorizontal(14).LineHorizontal(1).LineColor(Colors.Black);
                            f1.Item().PaddingTop(4).AlignCenter().Text("Firma del Cajero").Bold().FontSize(9);
                            f1.Item().AlignCenter().Text(cajeroTexto).FontSize(8).FontColor("#64748B");
                        });

                        firmasRow.ConstantItem(24);

                        firmasRow.RelativeItem().Column(f2 =>
                        {
                            f2.Item().PaddingHorizontal(14).LineHorizontal(1).LineColor(Colors.Black);
                            f2.Item().PaddingTop(4).AlignCenter().Text("Firma del Administrador").Bold().FontSize(9);
                            f2.Item().AlignCenter().Text("Conformidad y Recepción").FontSize(8).FontColor("#64748B");
                        });
                    });
                });

                // =========================================================
                // 3. PIE DE PÁGINA
                // =========================================================
                page.Footer().Column(footerCol =>
                {
                    footerCol.Item().LineHorizontal(0.5f).LineColor("#CBD5E1");
                    footerCol.Item().PaddingTop(4).AlignCenter()
                        .Text("Documento interno de control de caja — Ferretería ERP")
                        .FontSize(7.5f).Italic().FontColor("#64748B");
                });
            });
        }).GeneratePdf(rutaArchivo);
    }

    public void GenerarCotizacionPdf(Cotizacion cotizacion, List<DetalleCotizacion> detalles, string rutaArchivo)
    {
        ArgumentNullException.ThrowIfNull(cotizacion);
        if (string.IsNullOrWhiteSpace(rutaArchivo))
            throw new ArgumentException("Debe especificar una ruta válida para guardar el PDF.", nameof(rutaArchivo));

        var directorio = Path.GetDirectoryName(rutaArchivo);
        if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        var fechaLocal = cotizacion.FechaEmision.Kind == DateTimeKind.Utc
            ? cotizacion.FechaEmision.ToLocalTime()
            : cotizacion.FechaEmision;

        var nombreMes = CulturaBolivia.DateTimeFormat.GetMonthName(fechaLocal.Month);
        if (!string.IsNullOrEmpty(nombreMes))
            nombreMes = char.ToUpperInvariant(nombreMes[0]) + nombreMes.Substring(1);

        var fechaLiteral = $"Tarija, {fechaLocal:dd} de {nombreMes} de {fechaLocal:yyyy}";
        var nombreCliente = !string.IsNullOrWhiteSpace(cotizacion.NombreCliente)
            ? cotizacion.NombreCliente.Trim()
            : "S/N";
        var nitCiCliente = !string.IsNullOrWhiteSpace(cotizacion.NitCliente)
            ? cotizacion.NitCliente.Trim()
            : "0";

        var numeroProforma = cotizacion.Id > 0 ? cotizacion.Id.ToString("D4") : "0001";
        var montoLiteral = NumeroALetras.Convertir(cotizacion.Total, "BOLIVIANOS");

        var rutaLogoOpcional = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
        var existeLogo = File.Exists(rutaLogoOpcional);
        var datosNegocio = ObtenerDatosFiscales("FERRETERÍA ERP");
        var nombreNegocio = datosNegocio.NombreNegocio;
        var nitNegocio = datosNegocio.NitNegocio;
        var logoImagen = datosNegocio.LogoImagen;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(18, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#1E293B"));

                // =========================================================
                // 1. CABECERA DE LA PROFORMA / COTIZACIÓN
                // =========================================================
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        // Cabecera Izquierda: Logo + Nombre Empresa + Dirección
                        row.RelativeItem(3).Row(leftRow =>
                        {
                            leftRow.ConstantItem(56).Height(50).Border(1).BorderColor("#CBD5E1")
                                .Background("#F8FAFC").AlignCenter().AlignMiddle().Element(logoBox =>
                                {
                                    if (logoImagen is { Length: > 0 })
                                    {
                                        logoBox.Image(logoImagen).FitArea();
                                    }
                                    else if (existeLogo)
                                    {
                                        logoBox.Image(rutaLogoOpcional).FitArea();
                                    }
                                    else
                                    {
                                        logoBox.Text("LOGO\nFERRETERÍA")
                                            .FontSize(7.5f)
                                            .Bold()
                                            .FontColor("#0284C7")
                                            .AlignCenter();
                                    }
                                });

                            leftRow.ConstantItem(10);

                            leftRow.RelativeItem().Column(empCol =>
                            {
                                empCol.Item().Text(nombreNegocio.ToUpperInvariant()).FontSize(15).Bold().FontColor("#0F172A");
                                empCol.Item().Text("Materiales de Construcción, Herramientas y Pinturas")
                                    .FontSize(8.5f).FontColor("#475569");
                                empCol.Item().Text(datosNegocio.LineaContacto).FontSize(8.5f).SemiBold().FontColor("#1E293B");
                            });
                        });

                        row.ConstantItem(12);

                        // Cabecera Derecha: PROFORMA / COTIZACIÓN + DOCUMENTO NO VÁLIDO COMO FACTURA
                        row.ConstantItem(215).Border(1.2f).BorderColor(Colors.Black).Padding(8).Column(nitCol =>
                        {
                            if (!string.IsNullOrWhiteSpace(nitNegocio))
                            {
                                nitCol.Item().AlignCenter().Text($"NIT: {nitNegocio}").FontSize(9f).Bold().FontColor("#0F172A");
                            }
                            nitCol.Item().PaddingTop(string.IsNullOrWhiteSpace(nitNegocio) ? 0 : 2).AlignCenter()
                                .Text($"PROFORMA / COTIZACIÓN N° {numeroProforma}")
                                .FontSize(10.5f).Bold().FontColor("#0369A1");
                            nitCol.Item().PaddingTop(3).AlignCenter()
                                .Text("DOCUMENTO NO VÁLIDO COMO FACTURA")
                                .FontSize(7.8f).Bold().FontColor("#B91C1C");
                            nitCol.Item().PaddingTop(2).AlignCenter()
                                .Text($"Validez: {cotizacion.ValidezDias} días")
                                .FontSize(8.5f).SemiBold().FontColor("#334155");
                        });
                    });

                    headerCol.Item().PaddingTop(8).Border(1).BorderColor(Colors.Black).Padding(7).Column(cliCol =>
                    {
                        cliCol.Item().Row(r =>
                        {
                            r.RelativeItem(2).Text(txt =>
                            {
                                txt.Span("Lugar y Fecha: ").Bold();
                                txt.Span(fechaLiteral);
                            });

                            r.RelativeItem(1).AlignRight().Text(txt =>
                            {
                                txt.Span("NIT/CI: ").Bold();
                                txt.Span(nitCiCliente);
                            });
                        });

                        cliCol.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem(2).Text(txt =>
                            {
                                txt.Span("Señor(es): ").Bold();
                                txt.Span(nombreCliente);
                            });

                            r.RelativeItem(1).AlignRight().Text(txt =>
                            {
                                txt.Span("Vigencia hasta: ").Bold();
                                txt.Span($"{fechaLocal.AddDays(cotizacion.ValidezDias):dd/MM/yyyy}");
                            });
                        });
                    });
                });

                // =========================================================
                // 2. CUERPO: TABLA DE ITEMS COTIZADOS
                // =========================================================
                page.Content().PaddingTop(8).Column(bodyCol =>
                {
                    bodyCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(52); // CANT.
                            columns.RelativeColumn();   // CONCEPTO
                            columns.ConstantColumn(88); // PRECIO UNITARIO
                            columns.ConstantColumn(88); // TOTAL
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CeldaEncabezado).AlignCenter().Text("CANT.");
                            header.Cell().Element(CeldaEncabezado).Text("CONCEPTO");
                            header.Cell().Element(CeldaEncabezado).AlignRight().Text("PRECIO UNIT.");
                            header.Cell().Element(CeldaEncabezado).AlignRight().Text("TOTAL");

                            static IContainer CeldaEncabezado(IContainer container)
                            {
                                return container
                                    .Border(1)
                                    .BorderColor(Colors.Black)
                                    .Background("#E0F2FE")
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(6)
                                    .DefaultTextStyle(x => x.Bold().FontSize(9));
                            }
                        });

                        var items = detalles ?? cotizacion.Detalles?.ToList() ?? [];
                        foreach (var item in items)
                        {
                            var concepto = !string.IsNullOrWhiteSpace(item.Producto?.Nombre)
                                ? item.Producto.Nombre
                                : $"Producto #{item.IdProducto}";

                            table.Cell().Element(CeldaDetalle).AlignCenter().Text(item.Cantidad.ToString());
                            table.Cell().Element(CeldaDetalle).Text(concepto);
                            table.Cell().Element(CeldaDetalle).AlignRight().Text($"{item.PrecioUnitario:N2}");
                            table.Cell().Element(CeldaDetalle).AlignRight().Text($"{item.Subtotal:N2}");
                        }

                        static IContainer CeldaDetalle(IContainer container)
                        {
                            return container
                                .Border(0.75f)
                                .BorderColor(Colors.Black)
                                .PaddingVertical(4)
                                .PaddingHorizontal(6);
                        }
                    });

                    // =====================================================
                    // 3. TOTAL ESTIMADO LITERAL Y NUMÉRICO
                    // =====================================================
                    bodyCol.Item().Border(1).BorderColor(Colors.Black).Padding(6).Row(totRow =>
                    {
                        totRow.RelativeItem().AlignMiddle().Text(txt =>
                        {
                            txt.Span("SON: ").Bold();
                            txt.Span(montoLiteral);
                        });

                        totRow.ConstantItem(176).AlignRight().AlignMiddle().Text(txt =>
                        {
                            txt.Span("TOTAL Bs.: ").FontSize(10.5f).Bold();
                            txt.Span($"{cotizacion.Total:N2}").FontSize(11).Bold();
                        });
                    });
                });

                // =========================================================
                // 4. PIE DE PÁGINA: VALIDEZ Y MENSAJE PERSONALIZADO
                // =========================================================
                page.Footer().PaddingTop(4).Column(footerCol =>
                {
                    footerCol.Item().LineHorizontal(0.5f).LineColor("#94A3B8");
                    footerCol.Item().PaddingTop(3).AlignCenter().Text(
                            $"Presupuesto válido por {cotizacion.ValidezDias} días desde su emisión.  •  DOCUMENTO NO VÁLIDO COMO FACTURA.")
                        .FontSize(7.8f)
                        .Bold()
                        .FontColor("#334155");
                    footerCol.Item().PaddingTop(2).AlignCenter().Text(datosNegocio.MensajePieRecibo)
                        .FontSize(8f)
                        .Italic()
                        .FontColor("#475569");
                });
            });
        }).GeneratePdf(rutaArchivo);
    }
}

