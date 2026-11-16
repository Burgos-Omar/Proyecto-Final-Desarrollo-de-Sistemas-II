namespace FerreteriaERP.Core.Enums
{
    /// <summary>
    /// Define cómo se conecta esta instalación del sistema a la base de datos PostgreSQL.
    /// </summary>
    public enum ModoOperacion
    {
        /// <summary>Sucursal Central: aloja la base de datos, se conecta a "localhost".</summary>
        Central = 0,

        /// <summary>Sucursal secundaria: se conecta a la IP de la VPN (Tailscale/ZeroTier) de la Central.</summary>
        Sucursal = 1
    }
}
