using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FerreteriaERP.UI.Desktop.ViewModels;

namespace FerreteriaERP.UI.Desktop.Views;

public partial class PosView : UserControl
{
    private static readonly Uri QrAssetUri = new("avares://FerreteriaERP.UI.Desktop/Assets/qr_placeholder.png");
    private PosViewModel? _suscritoVm;

    public PosView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        CargarQrPersonalizadoSiExiste();
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

    private void CargarQrPersonalizadoSiExiste()
    {
        try
        {
            if (AssetLoader.Exists(QrAssetUri))
            {
                using var stream = AssetLoader.Open(QrAssetUri);
                ImgQrPago.Source = new Bitmap(stream);
            }
        }
        catch
        {
            // Mantiene el QR vectorial integrado en App.axaml si el archivo no existe o es inválido.
        }
    }

    private void EnfocarEscaneador()
    {
        Dispatcher.UIThread.Post(() => TxtCodigoBarras?.Focus(), DispatcherPriority.Input);
    }
}

