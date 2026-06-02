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

        SeedThemeResources();
    }

    /// <summary>
    ///     None yields a transparent brush even when resources are registered.
    /// </summary>
    [Test]
    public async Task Convert_NoneTag_ReturnsTransparentBrush()
    {
        var converter = TrafficFlowColorTagToBrushConverter.Instance;

        var result = converter.Convert(TrafficFlowColorTag.None, typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsEqualTo(Brushes.Transparent);
    }

    /// <summary>
    ///     Each defined color tag returns the brush registered in
    ///     <see cref="Application.Resources" /> under the converter's resource key.
    /// </summary>
    [Test]
    [Arguments(TrafficFlowColorTag.Red)]
    [Arguments(TrafficFlowColorTag.Orange)]
    [Arguments(TrafficFlowColorTag.Yellow)]
    [Arguments(TrafficFlowColorTag.Green)]
    [Arguments(TrafficFlowColorTag.Blue)]
    [Arguments(TrafficFlowColorTag.Purple)]
    [Arguments(TrafficFlowColorTag.Gray)]
    public async Task Convert_KnownTag_ReturnsBrushFromApplicationResources(TrafficFlowColorTag tag)
    {
        var converter = TrafficFlowColorTagToBrushConverter.Instance;
        var expectedKey = TrafficFlowColorTagBrushResources.BuildResourceKey(tag);
        var expected = Application.Current!.Resources[expectedKey];

        var result = converter.Convert(tag, typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsSameReferenceAs(expected);
    }

    /// <summary>
    ///     BuildResourceKey produces the documented "TrafficFlowColorTag.&lt;Tag&gt;.Brush" format.
    /// </summary>
    [Test]
    public async Task BuildResourceKey_KnownTag_ReturnsConventionalKey()
    {
        var key = TrafficFlowColorTagBrushResources.BuildResourceKey(TrafficFlowColorTag.Blue);

        await Assert.That(key).IsEqualTo("TrafficFlowColorTag.Blue.Brush");
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
    ///     When the resource is not registered, the converter falls back to transparent
    ///     instead of throwing or returning a hard-coded brush.
    /// </summary>
    [Test]
    public async Task Convert_MissingResource_ReturnsTransparent()
    {
        var converter = TrafficFlowColorTagToBrushConverter.Instance;
        var key = TrafficFlowColorTagBrushResources.BuildResourceKey(TrafficFlowColorTag.Red);
        var previous = Application.Current!.Resources[key];
        Application.Current.Resources.Remove(key);
        try
        {
            var result = converter.Convert(TrafficFlowColorTag.Red, typeof(IBrush), null, CultureInfo.InvariantCulture);

            await Assert.That(result).IsEqualTo(Brushes.Transparent);
        }
        finally
        {
            Application.Current.Resources[key] = previous;
        }
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

    private static void SeedThemeResources()
    {
        // Arbitrary distinct placeholder brushes — the converter is being tested,
        // not the production palette. The real palette lives in App.axaml under
        // ThemeDictionaries and is what end users see.
        var resources = Application.Current!.Resources;
        byte index = 1;
        foreach (var tag in System.Enum.GetValues<TrafficFlowColorTag>())
        {
            if (tag == TrafficFlowColorTag.None)
            {
                continue;
            }

            resources[TrafficFlowColorTagBrushResources.BuildResourceKey(tag)] =
                new SolidColorBrush(Color.FromRgb(index, index, index));
            index++;
        }
    }
}

/// <summary>
///     Minimal headless application used so the converter can resolve brushes
///     from <see cref="Application.Resources" /> during unit tests.
/// </summary>
internal sealed class ColorTagConverterHeadlessApp : Application;

