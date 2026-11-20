using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FerreteriaERP.UI.Desktop.ViewModels;
using FerreteriaERP.UI.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FerreteriaERP.UI.Desktop;

public partial class App : Application
{
    /// <summary>
    /// Contenedor de inyección de dependencias. Lo asigna <see cref="Program.Main"/> antes de iniciar Avalonia.
    /// Es null únicamente cuando el previsualizador (designer) arranca la app sin pasar por Main.
    /// </summary>
    public static IServiceProvider? Services { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // El ViewModel se obtiene del contenedor (con sus dependencias ya inyectadas).
            // Fallback para el designer, donde no existe el contenedor.
            var mainViewModel = Services?.GetRequiredService<MainViewModel>() ?? new MainViewModel();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };

            // Al cerrar la app se liberan los servicios que implementen IDisposable.
            desktop.Exit += (_, _) => (Services as IDisposable)?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}