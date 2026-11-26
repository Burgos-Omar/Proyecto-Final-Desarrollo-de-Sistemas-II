using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using FerreteriaERP.UI.Desktop.ViewModels;

namespace FerreteriaERP.UI.Desktop.Views;

public partial class PosView : UserControl
{
    private PosViewModel? _suscritoVm;

    public PosView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EnfocarEscaneador();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DesuscribirViewModel();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        DesuscribirViewModel();

        if (DataContext is PosViewModel nuevoVm)
        {
            _suscritoVm = nuevoVm;
            _suscritoVm.SolicitarFocoEscaneador += EnfocarEscaneador;
        }
    }

    private void DesuscribirViewModel()
    {
        if (_suscritoVm is not null)
        {
            _suscritoVm.SolicitarFocoEscaneador -= EnfocarEscaneador;
            _suscritoVm = null;
        }
    }

    private void EnfocarEscaneador()
    {
        Dispatcher.UIThread.Post(() => TxtCodigoBarras?.Focus(), DispatcherPriority.Input);
    }
}
