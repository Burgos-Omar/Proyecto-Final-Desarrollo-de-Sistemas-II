using FerreteriaERP.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FerreteriaERP.Infrastructure.Data
{
    /// <summary>
    /// Factory de contextos que lee la cadena de conexión EN CADA llamada.
    /// Así, si el usuario cambia entre "Central" y "Sucursal" en la pantalla de configuración,
    /// los repositorios usan la nueva conexión sin reiniciar la aplicación.
    /// (La clase <see cref="FerreteriaDbContextFactory"/> existente solo se usa en tiempo de diseño para las migraciones.)
    /// </summary>
    public class FerreteriaDbContextRuntimeFactory : IDbContextFactory<FerreteriaDbContext>
    {
        private readonly IConfiguracionConexionService _configuracion;

        public FerreteriaDbContextRuntimeFactory(IConfiguracionConexionService configuracion)
        {
            _configuracion = configuracion;
        }

        public FerreteriaDbContext CreateDbContext()
        {
            var opciones = new DbContextOptionsBuilder<FerreteriaDbContext>()
                .UseNpgsql(_configuracion.ConstruirCadenaConexion())
                .Options;

            return new FerreteriaDbContext(opciones);
        }
    }
}
