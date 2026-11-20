using System.Text.Json;
using System.Text.Json.Serialization;
using FerreteriaERP.Core.Configuration;
using FerreteriaERP.Core.Interfaces;
using Npgsql;

namespace FerreteriaERP.Infrastructure.Services
{
    /// <summary>
    /// Guarda la configuración de conexión en un JSON local por equipo:
    /// %APPDATA%\FerreteriaERP\conexion.json
    /// </summary>
    /// <remarks>
    /// OJO: la contraseña se guarda en texto plano en ese archivo. Para producción conviene cifrarla
    /// (por ejemplo con DPAPI: System.Security.Cryptography.ProtectedData en Windows).
    /// </remarks>
    public class ConfiguracionConexionService : IConfiguracionConexionService
    {
        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _rutaArchivo;
        private readonly object _candado = new();
        private ConfiguracionConexion _actual;

        public ConfiguracionConexionService()
        {
            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FerreteriaERP");
            Directory.CreateDirectory(carpeta);

            _rutaArchivo = Path.Combine(carpeta, "conexion.json");
            _actual = Cargar();
        }

        public ConfiguracionConexion Actual
        {
            get { lock (_candado) return _actual; }
        }

        public bool EstaConfigurado => File.Exists(_rutaArchivo);

        public void Guardar(ConfiguracionConexion configuracion)
        {
            lock (_candado)
            {
                File.WriteAllText(_rutaArchivo, JsonSerializer.Serialize(configuracion, OpcionesJson));
                _actual = configuracion; // Se activa de inmediato: la siguiente conexión ya usa estos datos.
            }
        }

        public string ConstruirCadenaConexion() => ConstruirCadenaConexion(Actual);

        public async Task<(bool Exitoso, string? Error)> ProbarConexionAsync(
            ConfiguracionConexion configuracion, CancellationToken ct = default)
        {
            try
            {
                await using var conexion = new NpgsqlConnection(ConstruirCadenaConexion(configuracion));
                await conexion.OpenAsync(ct);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static string ConstruirCadenaConexion(ConfiguracionConexion c)
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = c.ObtenerHost(),
                Port = c.Puerto,
                Database = c.BaseDatos,
                Username = c.Usuario,
                Password = c.Password,
                Timeout = 5,         // Segundos para conectar: falla rápido si la VPN está caída.
                CommandTimeout = 30  // Segundos máximos por consulta.
            };
            return builder.ConnectionString;
        }

        private ConfiguracionConexion Cargar()
        {
            try
            {
                if (File.Exists(_rutaArchivo))
                {
                    var json = File.ReadAllText(_rutaArchivo);
                    return JsonSerializer.Deserialize<ConfiguracionConexion>(json, OpcionesJson)
                           ?? new ConfiguracionConexion();
                }
            }
            catch (JsonException)
            {
                // Archivo corrupto: se ignora y se vuelve a pedir la configuración.
            }
            return new ConfiguracionConexion();
        }
    }
}
