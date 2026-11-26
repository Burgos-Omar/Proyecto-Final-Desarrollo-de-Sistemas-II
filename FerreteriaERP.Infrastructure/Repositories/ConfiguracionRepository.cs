using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Entities;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Repositories;

public class ConfiguracionRepository : Repository<ConfiguracionNegocio>, IConfiguracionRepository
{
    private static readonly string RutaRespaldoLocal = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FerreteriaERP",
        "configuracion_qr.json");

    public ConfiguracionRepository(IDbContextFactory<FerreteriaDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<ConfiguracionNegocio> ObtenerConfiguracionAsync(CancellationToken ct = default)
    {
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var config = await db.ConfiguracionesNegocio
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            if (config is not null)
                return config;
        }
        catch
        {
            // Si la tabla ConfiguracionesNegocio aún no fue migrada en PostgreSQL, lee el respaldo local
        }

        return LeerRespaldoLocal() ?? new ConfiguracionNegocio();
    }

    public async Task<ConfiguracionNegocio> GuardarQrPagoAsync(
        byte[]? imagen,
        string? banco,
        string? titular,
        CancellationToken ct = default)
    {
        var bancoLimpio = string.IsNullOrWhiteSpace(banco) ? null : banco.Trim();
        var titularLimpio = string.IsNullOrWhiteSpace(titular) ? null : titular.Trim();
        var ahoraUtc = DateTime.UtcNow;

        ConfiguracionNegocio resultado;
        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var existente = await db.ConfiguracionesNegocio.FirstOrDefaultAsync(ct);

            if (existente is null)
            {
                existente = new ConfiguracionNegocio
                {
                    NombreNegocio = "Ferretería ERP",
                    BancoQr = bancoLimpio,
                    TitularQr = titularLimpio,
                    QrPagoImagen = imagen,
                    FechaActualizacion = ahoraUtc
                };
                db.ConfiguracionesNegocio.Add(existente);
            }
            else
            {
                existente.BancoQr = bancoLimpio;
                existente.TitularQr = titularLimpio;
                existente.QrPagoImagen = imagen;
                existente.FechaActualizacion = ahoraUtc;
            }

            await db.SaveChangesAsync(ct);
            resultado = existente;
        }
        catch
        {
            var previo = LeerRespaldoLocal() ?? new ConfiguracionNegocio { Id = 1, NombreNegocio = "Ferretería ERP" };
            previo.BancoQr = bancoLimpio;
            previo.TitularQr = titularLimpio;
            previo.QrPagoImagen = imagen;
            previo.FechaActualizacion = ahoraUtc;
            resultado = previo;
        }

        GuardarRespaldoLocal(resultado);
        return resultado;
    }

    public async Task<ConfiguracionNegocio> GuardarDatosFiscalesAsync(
        string? nombreNegocio,
        string? nitNegocio,
        string? direccion = null,
        string? telefono = null,
        string? mensajePieRecibo = null,
        CancellationToken ct = default)
    {
        var nombreLimpio = string.IsNullOrWhiteSpace(nombreNegocio) ? "Ferretería ERP" : nombreNegocio.Trim();
        var nitLimpio = string.IsNullOrWhiteSpace(nitNegocio) ? null : nitNegocio.Trim();
        if (nitLimpio is not null && nitLimpio.Length > 30)
            nitLimpio = nitLimpio.Substring(0, 30);

        var direccionLimpia = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();
        var telefonoLimpio = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        var pieLimpio = string.IsNullOrWhiteSpace(mensajePieRecibo) ? null : mensajePieRecibo.Trim();

        var ahoraUtc = DateTime.UtcNow;
        ConfiguracionNegocio resultado;

        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var existente = await db.ConfiguracionesNegocio.FirstOrDefaultAsync(ct);

            if (existente is null)
            {
                existente = new ConfiguracionNegocio
                {
                    NombreNegocio = nombreLimpio,
                    NitNegocio = nitLimpio,
                    Direccion = direccionLimpia,
                    Telefono = telefonoLimpio,
                    MensajePieRecibo = pieLimpio,
                    FechaActualizacion = ahoraUtc
                };
                db.ConfiguracionesNegocio.Add(existente);
            }
            else
            {
                existente.NombreNegocio = nombreLimpio;
                existente.NitNegocio = nitLimpio;
                existente.Direccion = direccionLimpia;
                existente.Telefono = telefonoLimpio;
                existente.MensajePieRecibo = pieLimpio;
                existente.FechaActualizacion = ahoraUtc;
            }

            await db.SaveChangesAsync(ct);
            resultado = existente;
        }
        catch
        {
            var previo = LeerRespaldoLocal() ?? new ConfiguracionNegocio { Id = 1 };
            previo.NombreNegocio = nombreLimpio;
            previo.NitNegocio = nitLimpio;
            previo.Direccion = direccionLimpia;
            previo.Telefono = telefonoLimpio;
            previo.MensajePieRecibo = pieLimpio;
            previo.FechaActualizacion = ahoraUtc;
            resultado = previo;
        }

        GuardarRespaldoLocal(resultado);
        return resultado;
    }

    public async Task<ConfiguracionNegocio> GuardarLogoAsync(
        byte[]? logoBytes,
        string? nombreNegocio = null,
        CancellationToken ct = default)
    {
        var nombreLimpio = string.IsNullOrWhiteSpace(nombreNegocio) ? null : nombreNegocio.Trim();
        var ahoraUtc = DateTime.UtcNow;
        ConfiguracionNegocio resultado;

        try
        {
            await using var db = await ContextFactory.CreateDbContextAsync(ct);
            var existente = await db.ConfiguracionesNegocio.FirstOrDefaultAsync(ct);

            if (existente is null)
            {
                existente = new ConfiguracionNegocio
                {
                    NombreNegocio = nombreLimpio ?? "Ferretería ERP",
                    LogoImagen = logoBytes,
                    FechaActualizacion = ahoraUtc
                };
                db.ConfiguracionesNegocio.Add(existente);
            }
            else
            {
                existente.LogoImagen = logoBytes;
                if (nombreLimpio is not null)
                    existente.NombreNegocio = nombreLimpio;
                existente.FechaActualizacion = ahoraUtc;
            }

            await db.SaveChangesAsync(ct);
            resultado = existente;
        }
        catch
        {
            var previo = LeerRespaldoLocal() ?? new ConfiguracionNegocio { Id = 1, NombreNegocio = "Ferretería ERP" };
            previo.LogoImagen = logoBytes;
            if (nombreLimpio is not null)
                previo.NombreNegocio = nombreLimpio;
            previo.FechaActualizacion = ahoraUtc;
            resultado = previo;
        }

        GuardarRespaldoLocal(resultado);
        return resultado;
    }

    private static ConfiguracionNegocio? LeerRespaldoLocal()
    {
        try
        {
            if (!File.Exists(RutaRespaldoLocal))
                return null;

            var json = File.ReadAllText(RutaRespaldoLocal);
            return JsonSerializer.Deserialize<ConfiguracionNegocio>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void GuardarRespaldoLocal(ConfiguracionNegocio config)
    {
        try
        {
            var dir = Path.GetDirectoryName(RutaRespaldoLocal);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(RutaRespaldoLocal, json);
        }
        catch
        {
            // No bloquear el flujo si el archivo de respaldo local no puede escribirse
        }
    }
}
