using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using FerreteriaERP.UI.Desktop.ViewModels;

namespace FerreteriaERP.UI.Desktop.Views;

public partial class MainView : UserControl
{
    public static WindowNotificationManager? NotificationManagerActual { get; private set; }

    private WindowNotificationManager? _notificationManager;
    private MainViewModel? _viewModelSuscrito;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
        {
            _notificationManager = new WindowNotificationManager(topLevel)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 3
            };
            NotificationManagerActual = _notificationManager;
        }

        SuscribirViewModel(DataContext as MainViewModel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        SuscribirViewModel(null);
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        SuscribirViewModel(DataContext as MainViewModel);
    }

    private void SuscribirViewModel(MainViewModel? vm)
    {
        if (ReferenceEquals(_viewModelSuscrito, vm))
            return;

        if (_viewModelSuscrito is not null)
            _viewModelSuscrito.NotificacionExitoSolicitada -= MostrarNotificacionExito;

        _viewModelSuscrito = vm;

        if (_viewModelSuscrito is not null)
            _viewModelSuscrito.NotificacionExitoSolicitada += MostrarNotificacionExito;
    }

    private void MostrarNotificacionExito(string titulo, string mensaje)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _notificationManager?.Show(new Notification(
                titulo,
                mensaje,
                NotificationType.Success,
                TimeSpan.FromSeconds(5)));
        });
    }
}
