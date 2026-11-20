using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FerreteriaERP.Core.Entities;
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

    public TicketPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
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
                                    if (existeLogo)
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
                                empCol.Item().Text("FERRETERÍA").FontSize(15).Bold().FontColor("#0F172A");
                                empCol.Item().Text("Materiales de Construcción, Herramientas y Pinturas")
                                    .FontSize(8.5f).FontColor("#475569");
                                empCol.Item().Text("Tarija - Bolivia").FontSize(9).SemiBold().FontColor("#1E293B");
                            });
                        });

                        row.ConstantItem(12);

                        // Cabecera Derecha: Recuadro con NIT, NOTA DE VENTA N° 000X, ORIGINAL
                        row.ConstantItem(195).Border(1.2f).BorderColor(Colors.Black).Padding(8).Column(nitCol =>
                        {
                            nitCol.Item().AlignCenter().Text("NIT: 4120895014").FontSize(9.5f).Bold();
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
                // 4. PIE DE PÁGINA: LEYENDA LEGAL
                // =========================================================
                page.Footer().PaddingTop(4).Column(footerCol =>
                {
                    footerCol.Item().LineHorizontal(0.5f).LineColor("#94A3B8");
                    footerCol.Item().PaddingTop(3).AlignCenter().Text(
                            "\"La reproducción total o parcial y/o el uso no autorizado de esta Nota Fiscal, constituye un delito a ser sancionado conforme a Ley.\"")
                        .FontSize(7.5f)
                        .Italic()
                        .FontColor("#475569");
                });
            });
        }).GeneratePdf(rutaArchivo);
    }
}

