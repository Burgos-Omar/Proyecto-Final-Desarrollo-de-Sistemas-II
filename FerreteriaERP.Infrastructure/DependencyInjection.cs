using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure.Data;
using FerreteriaERP.Infrastructure.Repositories;
using FerreteriaERP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FerreteriaERP.Infrastructure
{
    /// <summary>
    /// Registro de todos los servicios de la capa de Infraestructura.
    /// La UI solo necesita llamar a <c>services.AddInfrastructure()</c>.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Configuración Central/Sucursal: una sola instancia compartida por toda la app.
            services.AddSingleton<IConfiguracionConexionService, ConfiguracionConexionService>();

            // Servicio de hashing y verificación de contraseñas (BCrypt).
            services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

            // Estado de sesión del usuario autenticado (Cajero/Admin y su Sucursal activa).
            services.AddSingleton<ISesionService, SesionService>();

            // Factory de contextos: crea un FerreteriaDbContext nuevo por operación leyendo la conexión vigente.
            services.AddSingleton<IDbContextFactory<FerreteriaDbContext>, FerreteriaDbContextRuntimeFactory>();

            // Servicio de importación masiva desde CSV/Excel.
            services.AddSingleton<IImportacionExcelService, ImportacionExcelService>();

            // Servicio de notificaciones y recordatorios de cobro por WhatsApp (wa.me).
            services.AddSingleton<IWhatsAppService, WhatsAppService>();

            // Servicio generador de Notas de Venta / Recibos en PDF (QuestPDF).
            services.AddSingleton<TicketPdfService>();

            // Servicio de respaldo de base de datos PostgreSQL (pg_dump).
            services.AddSingleton<BackupService>();

            // Servicio generador de Órdenes de Compra en PDF (QuestPDF).
            services.AddSingleton<OrdenCompraPdfService>();

            // Inyección directa de FerreteriaDbContext (con scope) por si algún servicio necesita
            // un contexto compartido durante una unidad de trabajo. Los repositorios NO lo usan.
            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<FerreteriaDbContext>>().CreateDbContext());

            // Repositorios: no guardan estado (crean su propio contexto por operación), por eso son Singleton.
            services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));
            services.AddSingleton<IProductoRepository, ProductoRepository>();
            services.AddSingleton<IVentaRepository, VentaRepository>();
            services.AddSingleton<IUsuarioRepository, UsuarioRepository>();
            services.AddSingleton<ICreditoRepository, CreditoRepository>();
            services.AddSingleton<IEntregaRepository, EntregaRepository>();
            services.AddSingleton<ISucursalRepository, SucursalRepository>();
            services.AddSingleton<IClienteRepository, ClienteRepository>();
            services.AddSingleton<ICotizacionRepository, CotizacionRepository>();
            services.AddSingleton<CotizacionRepository>();
            services.AddSingleton<IGastoOperativoRepository, GastoOperativoRepository>();
            services.AddSingleton<GastoOperativoRepository>();
            services.AddSingleton<IPromocionRepository, PromocionRepository>();
            services.AddSingleton<PromocionRepository>();
            services.AddSingleton<IOrdenCompraRepository, OrdenCompraRepository>();
            services.AddSingleton<OrdenCompraRepository>();
            services.AddSingleton<IConfiguracionRepository, ConfiguracionRepository>();
            services.AddSingleton<ConfiguracionRepository>();

            return services;
        }
    }
}
