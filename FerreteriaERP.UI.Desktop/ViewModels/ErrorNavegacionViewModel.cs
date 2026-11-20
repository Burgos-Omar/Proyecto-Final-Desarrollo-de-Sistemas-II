namespace FerreteriaERP.UI.Desktop.ViewModels;

/// <summary>
/// ViewModel de respaldo que muestra en pantalla un error de navegación en lugar de fallar en silencio.
/// </summary>
public class ErrorNavegacionViewModel : ViewModelBase
{
    public ErrorNavegacionViewModel(string mensaje)
    {
        Mensaje = mensaje;
    }

    public string Mensaje { get; }
}

