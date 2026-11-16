using Avalonia;
using System;
using FerreteriaERP.Core.Interfaces;
using FerreteriaERP.Infrastructure;
using FerreteriaERP.Infrastructure.Data;
using FerreteriaERP.UI.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FerreteriaERP.UI.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // Raíz de composición: se construye el contenedor de DI ANTES de arrancar Avalonia
        // y se entrega a la aplicación para que resuelva ViewModels y servicios desde ahí.
        var services = ConfigurarServicios();
        App.Services = services;

        // Siembra el usuario administrador inicial de forma segura si la conexión ya está configurada.
        InicializarBaseDeDatos(services);

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Ejecuta <see cref="DbSeeder.SeedInicial"/> dentro de un scope controlado si ya existe configuración de red.
    /// </summary>
    internal static void InicializarBaseDeDatos(IServiceProvider services)
    {
        var configuracion = services.GetRequiredService<IConfiguracionConexionService>();

        // Si todavía no existe conexion.json (primer arranque en limpio), se pospone hasta guardar la configuración.
        if (!configuracion.EstaConfigurado)
            return;

        try
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FerreteriaDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

            DbSeeder.SeedInicial(context, passwordHasher);
        }
        catch (Exception ex)
        {
            // Evita que un fallo temporal de red/BD cierre abruptamente la aplicación antes de abrir la ventana.
            Console.Error.WriteLine($"[DbSeeder] No se pudo ejecutar SeedInicial: {ex.Message}");
        }
    }

    /// <summary>
    /// Registra todas las dependencias de la aplicación (Infraestructura + ViewModels).
    /// </summary>
    private static IServiceProvider ConfigurarServicios()
    {
        var services = new ServiceCollection();

        // Capa Infraestructura: configuración de red, FerreteriaDbContext (vía factory), hashing, sesión, CSV, WhatsApp y repositorios.
        services.AddInfrastructure();

        // ViewModels: Transient = una instancia nueva cada vez que se piden (cada pantalla con su propio estado).
        services.AddTransient<MainViewModel>();
        services.AddTransient<ConfiguracionViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<InicioViewModel>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<InventarioViewModel>();
        services.AddTransient<ProveedoresViewModel>();
        services.AddTransient<LogisticaViewModel>();
        services.AddTransient<EmpleadosViewModel>();
        services.AddTransient<CreditosViewModel>();

        return services.BuildServiceProvider();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
