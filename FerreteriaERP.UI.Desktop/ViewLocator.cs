using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FerreteriaERP.UI.Desktop.ViewModels;

namespace FerreteriaERP.UI.Desktop;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            try
            {
                return (Control)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                // Hace visible el error real de la vista (XAML, binding, LiveCharts...) en vez de fallar en silencio
                var detalle = (ex.InnerException ?? ex).ToString();
                System.Diagnostics.Debug.WriteLine($"[ViewLocator] Error al crear {name}: {detalle}");
                return new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = $"Error al cargar la vista {type.Name}:\n\n{detalle}",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Avalonia.Thickness(28)
                    }
                };
            }
        }
        
        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
