using FerreteriaERP.Core.Enums;

namespace FerreteriaERP.Core.Configuration
{
    /// <summary>
    /// Datos de conexión de esta instalación. Se captura en la pantalla de configuración
    /// inicial y se persiste localmente en cada equipo (no en la base de datos).
    /// </summary>
    public class ConfiguracionConexion
    {
        /// <summary>Central (localhost) o Sucursal (IP de la VPN).</summary>
        public ModoOperacion Modo { get; set; } = ModoOperacion.Central;

        /// <summary>IP o nombre del servidor. Solo se usa en modo Sucursal (ej. 100.64.0.1 de Tailscale).</summary>
        public string Servidor { get; set; } = string.Empty;

        public int Puerto { get; set; } = 5432;
        public string BaseDatos { get; set; } = "FerreteriaDSS";
        public string Usuario { get; set; } = "postgres";
        public string Password { get; set; } = string.Empty;

        /// <summary>Host efectivo: "localhost" para la Central, la IP de la VPN para una Sucursal.</summary>
        public string ObtenerHost() =>
            Modo == ModoOperacion.Central || string.IsNullOrWhiteSpace(Servidor)
                ? "localhost"
                : Servidor.Trim();
    }
}
