using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace FerreteriaERP.Infrastructure.Services;

/// <summary>
/// Servicio encargado de generar copias de seguridad completas (.backup) de PostgreSQL mediante pg_dump.
/// </summary>
public class BackupService
{
    public async Task GenerarRespaldoAsync(string rutaDestino, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(rutaDestino))
            throw new ArgumentException("La ruta de destino del respaldo no puede estar vacía.", nameof(rutaDestino));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("La cadena de conexión no puede estar vacía.", nameof(connectionString));

        var directorio = Path.GetDirectoryName(rutaDestino);
        if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var host = string.IsNullOrWhiteSpace(builder.Host) ? "localhost" : builder.Host;
        var port = builder.Port > 0 ? builder.Port : 5432;
        var database = string.IsNullOrWhiteSpace(builder.Database) ? "FerreteriaDSS" : builder.Database;
        var user = string.IsNullOrWhiteSpace(builder.Username) ? "postgres" : builder.Username;
        var password = builder.Password ?? string.Empty;

        var ejecutablePgDump = ResolverRutaPgDump();

        var processInfo = new ProcessStartInfo
        {
            FileName = ejecutablePgDump,
            Arguments = $"-h {host} -p {port} -U {user} -F c -f \"{rutaDestino}\" {database}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        processInfo.EnvironmentVariables["PGPASSWORD"] = password;

        using var process = new Process { StartInfo = processInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("No se pudo iniciar el proceso pg_dump.");
        }

        var errorOutputTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var errorDetalle = await errorOutputTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"pg_dump finalizó con código {process.ExitCode}: {errorDetalle}");
        }
    }

    private static string ResolverRutaPgDump()
    {
        // En Windows, si pg_dump no está en el PATH del sistema, busca en la instalación estándar de PostgreSQL
        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var carpetaPostgres = Path.Combine(programFiles, "PostgreSQL");
            if (Directory.Exists(carpetaPostgres))
            {
                var candidato = Directory.GetDirectories(carpetaPostgres)
                    .OrderByDescending(d => d)
                    .Select(d => Path.Combine(d, "bin", "pg_dump.exe"))
                    .FirstOrDefault(File.Exists);

                if (!string.IsNullOrEmpty(candidato))
                    return candidato;
            }
        }

        return "pg_dump";
    }
}
