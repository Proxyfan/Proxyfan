using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Proxyfan.Client.Traffic.Converters;
using Proxyfan.Domain.Traffic;
using System.Globalization;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="TrafficFlowColorTagToBrushConverter" />.
/// </summary>
[NotInParallel]
public sealed class TrafficFlowColorTagToBrushConverterTests
{
    static TrafficFlowColorTagToBrushConverterTests()
    {
        if (Application.Current is null)
        {
            AppBuilder.Configure<ColorTagConverterHeadlessApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                .SetupWithoutStarting();
        }
    }

    /// <summary>
    ///     None yields a transparent brush.
    /// </summary>
    [Test]
    public async Task Convert_NoneTag_ReturnsTransparentBrush()
    {
        var converter = TrafficFlowColorTagToBrushConverter.Instance;

        var result = converter.Convert(TrafficFlowColorTag.None, typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsEqualTo(Brushes.Transparent);
    }

    /// <summary>
    ///     Each defined color tag resolves the brush from application resources.
    /// </summary>
    [Test]
    [Arguments(TrafficFlowColorTag.Red)]
    [Arguments(TrafficFlowColorTag.Orange)]
    [Arguments(TrafficFlowColorTag.Yellow)]
    [Arguments(TrafficFlowColorTag.Green)]
    [Arguments(TrafficFlowColorTag.Blue)]
    [Arguments(TrafficFlowColorTag.Purple)]
    [Arguments(TrafficFlowColorTag.Gray)]
    public async Task Convert_KnownTag_ReturnsResourceBrush(TrafficFlowColorTag tag)
    {
        var converter = TrafficFlowColorTagToBrushConverter.Instance;
        var resourceKey = TrafficFlowColorTagBrushKeys.GetResourceKey(tag);
        Application.Current!.TryGetResource(resourceKey!, Application.Current.ActualThemeVariant, out var expected);

        var result = converter.Convert(tag, typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsSameReferenceAs(expected);
        await Assert.That(result).IsNotEqualTo(Brushes.Transparent);
    }

    /// <summary>
    ///     When the resource is missing the converter falls back to transparent.
    /// </summary>
    [Test]
    public async Task Convert_MissingResource_ReturnsTransparent()
    {
        var converter = TrafficFlowColorTagToBrushConverter.Instance;
        var key = TrafficFlowColorTagBrushKeys.Red;
        var previous = Application.Current!.Resources[key];
        Application.Current.Resources.Remove(key);
        try
        {
            var result = converter.Convert(TrafficFlowColorTag.Red, typeof(IBrush), null, CultureInfo.InvariantCulture);

            await Assert.That(result).IsEqualTo(Brushes.Transparent);
        }
        finally
        {
            if (previous is not null)
            {
                Application.Current.Resources[key] = previous;
            }
        }
    }

    /// <summary>
    ///     Non-tag inputs fall through to transparent.
    /// </summary>
    [Test]
    public async Task Convert_NonTagInput_ReturnsTransparent()
    {
        var converter = TrafficFlowColorTagToBrushConverter.Instance;

        var result = converter.Convert("not-a-tag", typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsEqualTo(Brushes.Transparent);
    }

    /// <summary>
    ///     Each known tag has a non-null resource key.
    /// </summary>
    [Test]
    [Arguments(TrafficFlowColorTag.Red)]
    [Arguments(TrafficFlowColorTag.Orange)]
    [Arguments(TrafficFlowColorTag.Yellow)]
    [Arguments(TrafficFlowColorTag.Green)]
    [Arguments(TrafficFlowColorTag.Blue)]
    [Arguments(TrafficFlowColorTag.Purple)]
    [Arguments(TrafficFlowColorTag.Gray)]
    public async Task GetResourceKey_KnownTag_ReturnsNonNullKey(TrafficFlowColorTag tag)
    {
        var key = TrafficFlowColorTagBrushKeys.GetResourceKey(tag);

        await Assert.That(key).IsNotNull();
    }

    /// <summary>
    ///     None maps to a null resource key.
    /// </summary>
    [Test]
    public async Task GetResourceKey_NoneTag_ReturnsNull()
    {
        var key = TrafficFlowColorTagBrushKeys.GetResourceKey(TrafficFlowColorTag.None);

        await Assert.That(key).IsNull();
    }

    /// <summary>
    ///     ConvertBack is unsupported and throws.
    /// </summary>
    [Test]
    public async Task ConvertBack_AnyInput_Throws()
    {
        var converter = TrafficFlowColorTagToBrushConverter.Instance;

        await Assert.That(() => converter.ConvertBack(Brushes.Red, typeof(TrafficFlowColorTag), null, CultureInfo.InvariantCulture))
            .Throws<System.NotSupportedException>();
    }
}

/// <summary>
///     Minimal headless application that registers the same color-tag brush
///     resources as the production <c>App.axaml</c> so the converter can
///     resolve them under test without booting the full client host.
/// </summary>
internal sealed class ColorTagConverterHeadlessApp : Application
{
    public override void Initialize()
    {
        Resources[TrafficFlowColorTagBrushKeys.Red] = new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C));
        Resources[TrafficFlowColorTagBrushKeys.Orange] = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00));
        Resources[TrafficFlowColorTagBrushKeys.Yellow] = new SolidColorBrush(Color.FromRgb(0xDA, 0xA5, 0x20));
        Resources[TrafficFlowColorTagBrushKeys.Green] = new SolidColorBrush(Color.FromRgb(0x22, 0x8B, 0x22));
        Resources[TrafficFlowColorTagBrushKeys.Blue] = new SolidColorBrush(Color.FromRgb(0x1E, 0x90, 0xFF));
        Resources[TrafficFlowColorTagBrushKeys.Purple] = new SolidColorBrush(Color.FromRgb(0x93, 0x70, 0xDB));
        Resources[TrafficFlowColorTagBrushKeys.Gray] = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
    }
}
