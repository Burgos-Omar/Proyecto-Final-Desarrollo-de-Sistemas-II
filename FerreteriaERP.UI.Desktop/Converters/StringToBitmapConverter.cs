using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace FerreteriaERP.UI.Desktop.Converters;

/// <summary>
/// Convierte una ruta local de imagen (<see cref="string"/>) en un <see cref="Bitmap"/> de Avalonia.
/// Si la ruta es nula, vacía o el archivo no existe en disco, devuelve el placeholder por defecto
/// (<c>avares://FerreteriaERP.UI.Desktop/Assets/placeholder.png</c>).
/// </summary>
public class StringToBitmapConverter : IValueConverter
{
    public static readonly StringToBitmapConverter Instance = new();

    private static readonly Uri PlaceholderUri =
        new("avares://FerreteriaERP.UI.Desktop/Assets/placeholder.png");

    private static Bitmap? _placeholderCache;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string filePath && !string.IsNullOrWhiteSpace(filePath))
        {
            try
            {
                // 1. Si es una ruta de recurso embebido avares://
                if (filePath.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
                {
                    var resourceUri = new Uri(filePath);
                    if (AssetLoader.Exists(resourceUri))
                    {
                        using var resourceStream = AssetLoader.Open(resourceUri);
                        return new Bitmap(resourceStream);
                    }
                }
                // 2. Si es una ruta local en el disco duro (%APPDATA%\FerreteriaERP\Imagenes\...)
                else if (File.Exists(filePath))
                {
                    using var fileStream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite);
                    return new Bitmap(fileStream);
                }
            }
            catch
            {
                // Si el archivo está corrupto o bloqueado, cae de forma segura al placeholder.
            }
        }

        return ObtenerPlaceholderBitmap();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Bitmap? ObtenerPlaceholderBitmap()
    {
        if (_placeholderCache is not null)
            return _placeholderCache;

        try
        {
            if (AssetLoader.Exists(PlaceholderUri))
            {
                using var stream = AssetLoader.Open(PlaceholderUri);
                _placeholderCache = new Bitmap(stream);
                return _placeholderCache;
            }
        }
        catch
        {
            // Ignorado: si placeholder.png no está presente, generamos un bitmap neutro en memoria.
        }

        // Respaldo seguro en memoria (160x120 gris claro #F1F5F9) por si aún no existe Assets/placeholder.png
        _placeholderCache = CrearBitmapRespaldoEnMemoria();
        return _placeholderCache;
    }

    private static Bitmap CrearBitmapRespaldoEnMemoria()
    {
        // PNG 1x1 válido codificado en Base64 (#F1F5F9) para evitar excepciones si falta el archivo físico
        var pngBytes = System.Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mPce/b8fwAH0gM85r16vAAAAABJRU5ErkJggg==");
        using var ms = new MemoryStream(pngBytes);
        return new Bitmap(ms);
    }
}

