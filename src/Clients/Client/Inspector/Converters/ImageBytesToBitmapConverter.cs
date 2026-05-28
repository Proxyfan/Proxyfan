using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;

namespace Proxyfan.Client.Inspector.Converters;

/// <summary>
///     Converts a byte array containing an encoded image (PNG, JPEG, GIF, BMP)
///     to an Avalonia <see cref="Bitmap" /> for display. Returns <see langword="null" /> when
///     the input is null, empty, or cannot be decoded.
/// </summary>
public sealed class ImageBytesToBitmapConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes)
        {
            return null;
        }

        if (bytes.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ImageBytesToBitmapConverter does not support conversion back.");
    }
}
