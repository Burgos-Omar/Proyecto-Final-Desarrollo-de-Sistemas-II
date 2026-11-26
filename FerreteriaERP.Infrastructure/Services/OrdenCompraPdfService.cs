using System;
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
/// Generador profesional de Órdenes de Compra en PDF utilizando QuestPDF (PASO 30).
/// </summary>
public class OrdenCompraPdfService
{
    private readonly IConfiguracionRepository? _configuracionRepository;

    public OrdenCompraPdfService(IConfiguracionRepository? configuracionRepository = null)
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

    private DatosNegocioPdf ObtenerDatosFiscales()
    {
        const string contactoFallback = "Tarija - Bolivia";
        const string pieFallback = "¡Gracias por su preferencia!";

        if (_configuracionRepository is null)
            return new DatosNegocioPdf("FERRETERÍA ERP", null, null, contactoFallback, pieFallback);

        try
        {
            var config = _configuracionRepository.ObtenerConfiguracionAsync().GetAwaiter().GetResult();
            var nombre = string.IsNullOrWhiteSpace(config?.NombreNegocio)
                ? "FERRETERÍA ERP"
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
            return new DatosNegocioPdf("FERRETERÍA ERP", null, null, contactoFallback, pieFallback);
        }
    }

    public void GenerarOrdenCompraPdf(OrdenCompra orden, string rutaArchivo)
    {
        ArgumentNullException.ThrowIfNull(orden);
        if (string.IsNullOrWhiteSpace(rutaArchivo))
            throw new ArgumentException("Debe especificar una ruta válida para guardar el PDF.", nameof(rutaArchivo));

        var directorio = Path.GetDirectoryName(rutaArchivo);
        if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        var fechaLocal = orden.FechaEmision.Kind == DateTimeKind.Utc
            ? orden.FechaEmision.ToLocalTime()
            : orden.FechaEmision;

        var numeroOrden = string.IsNullOrWhiteSpace(orden.NumeroOrden)
            ? $"OC-{fechaLocal:yyyy}-{orden.Id:D4}"
            : orden.NumeroOrden;

        var nombreProveedor = orden.Proveedor?.NombreComercialMostrar ?? $"Proveedor #{orden.IdProveedor}";
        var nitProveedor = orden.Proveedor?.NitMostrar ?? "Sin NIT registrado";
        var telProveedor = orden.Proveedor?.TelefonoMostrar ?? "Sin teléfono";
        var dirProveedor = string.IsNullOrWhiteSpace(orden.Proveedor?.DireccionAlmacen)
            ? "Tarija - Bolivia"
            : orden.Proveedor.DireccionAlmacen!;
        var nombreSucursal = orden.Sucursal?.Nombre ?? "Sucursal Central";
        var montoLiteral = NumeroALetras.Convertir(orden.TotalEstimado, "BOLIVIANOS");

        var rutaLogoOpcional = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
        var existeLogo = File.Exists(rutaLogoOpcional);
        var datosNegocio = ObtenerDatosFiscales();
        var nombreNegocio = datosNegocio.NombreNegocio;
        var nitNegocio = datosNegocio.NitNegocio;
        var logoImagen = datosNegocio.LogoImagen;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Portrait());
                page.Margin(28, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#1E293B"));

                // =========================================================
                // 1. ENCABEZADO PROFESIONAL
                // =========================================================
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        // Datos de la Ferretería (Izquierda)
                        row.RelativeItem(3).Row(leftRow =>
                        {
                            leftRow.ConstantItem(60).Height(54).Border(1).BorderColor("#CBD5E1")
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
                                            .FontSize(8f)
                                            .Bold()
                                            .FontColor("#059669")
                                            .AlignCenter();
                                    }
                                });

                            leftRow.ConstantItem(12);

                            leftRow.RelativeItem().Column(empCol =>
                            {
                                empCol.Item().Text(nombreNegocio.ToUpperInvariant()).FontSize(16).Bold().FontColor("#0F172A");
                                if (!string.IsNullOrWhiteSpace(nitNegocio))
                                {
                                    empCol.Item().Text($"NIT: {nitNegocio}")
                                        .FontSize(9f).Bold().FontColor("#334155");
                                }
                                empCol.Item().Text("Departamento de Compras y Abastecimiento")
                                    .FontSize(9f).FontColor("#475569");
                                empCol.Item().Text($"{datosNegocio.LineaContacto}  •  Destino: {nombreSucursal}")
                                    .FontSize(8.5f).SemiBold().FontColor("#1E293B");
                            });
                        });

                        row.ConstantItem(14);

                        // Recuadro Derecho: ORDEN DE COMPRA + Nro + Fecha
                        row.ConstantItem(205).Border(1.2f).BorderColor("#0F172A").Background("#F8FAFC").Padding(10).Column(ocCol =>
                        {
                            ocCol.Item().AlignCenter()
                                .Text("ORDEN DE COMPRA")
                                .FontSize(12.5f).Bold().FontColor("#0F172A");
                            ocCol.Item().PaddingTop(3).AlignCenter()
                                .Text($"N° {numeroOrden}")
                                .FontSize(11).Bold().FontColor("#059669");
                            ocCol.Item().PaddingTop(3).AlignCenter()
                                .Text($"Fecha: {fechaLocal:dd/MM/yyyy HH:mm}")
                                .FontSize(8.5f).FontColor("#475569");
                        });
                    });

                    // Datos del Proveedor
                    headerCol.Item().PaddingTop(12).Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(10).Column(provCol =>
                    {
                        provCol.Item().Text("DATOS DEL PROVEEDOR").FontSize(8.5f).Bold().FontColor("#059669");
                        provCol.Item().PaddingTop(4).Row(r1 =>
                        {
                            r1.RelativeItem(2).Text(t =>
                            {
                                t.Span("Nombre Comercial: ").Bold();
                                t.Span(nombreProveedor);
                            });
                            r1.RelativeItem(1).AlignRight().Text(t =>
                            {
                                t.Span("NIT: ").Bold();
                                t.Span(nitProveedor.Replace("NIT: ", string.Empty));
                            });
                        });
                        provCol.Item().PaddingTop(3).Row(r2 =>
                        {
                            r2.RelativeItem(2).Text(t =>
                            {
                                t.Span("Dirección: ").Bold();
                                t.Span(dirProveedor);
                            });
                            r2.RelativeItem(1).AlignRight().Text(t =>
                            {
                                t.Span("Teléfono: ").Bold();
                                t.Span(telProveedor);
                            });
                        });
                    });
                });

                // =========================================================
                // 2. TABLA DETALLADA DE PRODUCTOS SOLICITADOS
                // =========================================================
                page.Content().PaddingTop(14).Column(bodyCol =>
                {
                    bodyCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(78);  // CÓDIGO
                            columns.RelativeColumn();    // PRODUCTO
                            columns.ConstantColumn(68);  // CANTIDAD
                            columns.ConstantColumn(96);  // COSTO UNIT. EST.
                            columns.ConstantColumn(96);  // SUBTOTAL
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CeldaHeader).Text("CÓDIGO");
                            header.Cell().Element(CeldaHeader).Text("PRODUCTO");
                            header.Cell().Element(CeldaHeader).AlignCenter().Text("CANTIDAD");
                            header.Cell().Element(CeldaHeader).AlignRight().Text("COSTO UNIT. (Bs.)");
                            header.Cell().Element(CeldaHeader).AlignRight().Text("SUBTOTAL (Bs.)");

                            static IContainer CeldaHeader(IContainer c) =>
                                c.Border(1)
                                 .BorderColor("#0F172A")
                                 .Background("#E2E8F0")
                                 .PaddingVertical(6)
                                 .PaddingHorizontal(6)
                                 .DefaultTextStyle(x => x.Bold().FontSize(8.5f));
                        });

                        var detalles = orden.Detalles?.ToList() ?? [];
                        foreach (var item in detalles)
                        {
                            var codigo = !string.IsNullOrWhiteSpace(item.Producto?.CodigoBarras)
                                ? item.Producto.CodigoBarras!
                                : $"PROD-{item.IdProducto}";
                            var nombre = !string.IsNullOrWhiteSpace(item.Producto?.Nombre)
                                ? item.Producto.Nombre
                                : $"Producto #{item.IdProducto}";

                            table.Cell().Element(CeldaBody).Text(codigo);
                            table.Cell().Element(CeldaBody).Text(nombre);
                            table.Cell().Element(CeldaBody).AlignCenter().Text(item.CantidadSolicitada.ToString());
                            table.Cell().Element(CeldaBody).AlignRight().Text($"{item.CostoUnitarioEstimado:N2}");
                            table.Cell().Element(CeldaBody).AlignRight().Text($"{item.Subtotal:N2}");
                        }

                        static IContainer CeldaBody(IContainer c) =>
                            c.Border(0.75f)
                             .BorderColor("#94A3B8")
                             .PaddingVertical(5)
                             .PaddingHorizontal(6);
                    });

                    // Total Estimado (Numérico y Literal)
                    bodyCol.Item().Border(1).BorderColor("#0F172A").Background("#ECFDF5").Padding(8).Row(totRow =>
                    {
                        totRow.RelativeItem().AlignMiddle().Text(t =>
                        {
                            t.Span("SON: ").Bold().FontSize(9f);
                            t.Span(montoLiteral).FontSize(9f);
                        });

                        totRow.ConstantItem(190).AlignRight().AlignMiddle().Text(t =>
                        {
                            t.Span("TOTAL ESTIMADO: ").FontSize(10f).Bold().FontColor("#065F46");
                            t.Span($"Bs. {orden.TotalEstimado:N2}").FontSize(11.5f).Bold().FontColor("#065F46");
                        });
                    });

                    // Observaciones
                    bodyCol.Item().PaddingTop(12).Border(1).BorderColor("#CBD5E1").Padding(9).Column(obsCol =>
                    {
                        obsCol.Item().Text("OBSERVACIONES / CONDICIONES DE ENTREGA").FontSize(8.5f).Bold().FontColor("#475569");
                        obsCol.Item().PaddingTop(3).Text(
                            string.IsNullOrWhiteSpace(orden.Observaciones)
                                ? "Sin observaciones adicionales. Entregar en almacén de la sucursal de destino con guía de remisión."
                                : orden.Observaciones)
                            .FontSize(9f);
                    });

                    // Firmas de Autorización
                    bodyCol.Item().PaddingTop(65).Row(firmasRow =>
                    {
                        firmasRow.RelativeItem().Column(f1 =>
                        {
                            f1.Item().PaddingHorizontal(24).LineHorizontal(1).LineColor("#0F172A");
                            f1.Item().PaddingTop(4).AlignCenter().Text("Autorizado por Administración").Bold().FontSize(9f);
                            f1.Item().AlignCenter().Text("Firma y Sello").FontSize(8f).FontColor("#64748B");
                        });

                        firmasRow.ConstantItem(36);

                        firmasRow.RelativeItem().Column(f2 =>
                        {
                            f2.Item().PaddingHorizontal(24).LineHorizontal(1).LineColor("#0F172A");
                            f2.Item().PaddingTop(4).AlignCenter().Text("Responsable de Almacén").Bold().FontSize(9f);
                            f2.Item().AlignCenter().Text("Control y Recepción").FontSize(8f).FontColor("#64748B");
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
                        .Text($"Orden de Compra {numeroOrden} — {nombreNegocio} ({datosNegocio.LineaContacto})")
                        .FontSize(7.5f).Italic().FontColor("#64748B");
                });
            });
        }).GeneratePdf(rutaArchivo);
    }
}
