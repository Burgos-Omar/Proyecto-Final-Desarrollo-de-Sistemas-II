using System.Threading;
using System.Threading.Tasks;
using FerreteriaERP.Core.Configuration;

namespace FerreteriaERP.Core.Interfaces
{
    /// <summary>
    /// Lee y guarda la configuración de red (Central / Sucursal) y construye la cadena de conexión.
    /// </summary>
    public interface IConfiguracionConexionService
    {
        /// <summary>Configuración vigente (valores por defecto si todavía no se ha configurado).</summary>
        ConfiguracionConexion Actual { get; }

        /// <summary>Indica si ya existe una configuración guardada (si no, mostrar la pantalla inicial).</summary>
        bool EstaConfigurado { get; }

        /// <summary>Persiste la configuración y la activa de inmediato.</summary>
        void Guardar(ConfiguracionConexion configuracion);

        /// <summary>Construye la cadena de conexión de Npgsql a partir de la configuración vigente.</summary>
        string ConstruirCadenaConexion();

        /// <summary>Intenta abrir una conexión con la configuración dada (para el botón "Probar conexión").</summary>
        Task<(bool Exitoso, string? Error)> ProbarConexionAsync(
            ConfiguracionConexion configuracion, CancellationToken ct = default);
    }
}
