using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FerreteriaERP.UI.Desktop.ViewModels;

namespace FerreteriaERP.UI.Desktop.Views;

/// <summary>Vista mínima (sin XAML) que muestra el mensaje de <see cref="ErrorNavegacionViewModel"/>.</summary>
public class ErrorNavegacionView : UserControl
{
    private readonly TextBlock _texto = new()
    {
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brushes.Firebrick,
        Margin = new Thickness(28)
    };

    public ErrorNavegacionView()
    {
        Content = _texto;
        DataContextChanged += (_, _) =>
            _texto.Text = (DataContext as ErrorNavegacionViewModel)?.Mensaje ?? string.Empty;
    }
}

