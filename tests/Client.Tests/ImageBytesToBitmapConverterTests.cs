using Avalonia.Headless;
using Avalonia;
using Avalonia.Media.Imaging;
using Proxyfan.Client.Inspector.Converters;
using System.Globalization;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ImageBytesToBitmapConverter" />.
/// </summary>
[NotInParallel]
public sealed class ImageBytesToBitmapConverterTests
{
    static ImageBytesToBitmapConverterTests()
    {
        if (Application.Current is null)
        {
            AppBuilder.Configure<ConverterHeadlessApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                .SetupWithoutStarting();
        }
    }

    [Test]
    public async Task Convert_NullValue_ReturnsNull()
    {
        var converter = new ImageBytesToBitmapConverter();

        var result = converter.Convert(null, typeof(Bitmap), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Convert_NonByteArrayValue_ReturnsNull()
    {
        var converter = new ImageBytesToBitmapConverter();

        var result = converter.Convert("not bytes", typeof(Bitmap), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Convert_EmptyByteArray_ReturnsNull()
    {
        var converter = new ImageBytesToBitmapConverter();

        var result = converter.Convert(System.Array.Empty<byte>(), typeof(Bitmap), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Convert_ValidPngBytes_ReturnsBitmap()
    {
        var converter = new ImageBytesToBitmapConverter();
        var pngBytes = CreateMinimalPng();

        var result = converter.Convert(pngBytes, typeof(Bitmap), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsTypeOf<Bitmap>();
    }

    [Test]
    public async Task ConvertBack_NullArguments_ThrowsNotSupportedException()
    {
        var converter = new ImageBytesToBitmapConverter();

        await Assert.That(() => converter.ConvertBack(null, typeof(byte[]), null, CultureInfo.InvariantCulture))
            .Throws<System.NotSupportedException>();
    }

    private static byte[] CreateMinimalPng()
    {
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00,
            0x90, 0x77, 0x53, 0xDE,
            0x00, 0x00, 0x00, 0x0C,
            0x49, 0x44, 0x41, 0x54,
            0x08, 0x99, 0x63, 0xF8, 0xCF, 0xC0, 0x00, 0x00, 0x00, 0x03, 0x00, 0x01,
            0x5B, 0x1F, 0xC4, 0xF6,
            0x00, 0x00, 0x00, 0x00,
            0x49, 0x45, 0x4E, 0x44,
            0xAE, 0x42, 0x60, 0x82,
        };
    }
}

/// <summary>
///     Minimal headless application for the converter test fixture.
/// </summary>
internal sealed class ConverterHeadlessApp : Application;

