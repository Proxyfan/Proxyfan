using Avalonia.Media;
using Proxyfan.Client.Traffic.Converters;
using Proxyfan.Domain.Traffic;
using System.Globalization;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="TrafficFlowColorTagToBrushConverter" />.
/// </summary>
public sealed class TrafficFlowColorTagToBrushConverterTests
{
    /// <summary>
    ///     None yields a transparent brush even when every styled property is set.
    /// </summary>
    [Test]
    public async Task Convert_NoneTag_ReturnsTransparentBrush()
    {
        var converter = BuildConverter();

        var result = converter.Convert(TrafficFlowColorTag.None, typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsEqualTo(Brushes.Transparent);
    }

    /// <summary>
    ///     Each defined color tag returns the brush assigned to the matching
    ///     styled property.
    /// </summary>
    [Test]
    [Arguments(TrafficFlowColorTag.Red)]
    [Arguments(TrafficFlowColorTag.Orange)]
    [Arguments(TrafficFlowColorTag.Yellow)]
    [Arguments(TrafficFlowColorTag.Green)]
    [Arguments(TrafficFlowColorTag.Blue)]
    [Arguments(TrafficFlowColorTag.Purple)]
    [Arguments(TrafficFlowColorTag.Gray)]
    public async Task Convert_KnownTag_ReturnsBrushFromMatchingStyledProperty(TrafficFlowColorTag tag)
    {
        var converter = BuildConverter();
        var expected = GetExpectedBrush(converter, tag);

        var result = converter.Convert(tag, typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsSameReferenceAs(expected);
    }

    /// <summary>
    ///     Non-tag inputs fall through to transparent.
    /// </summary>
    [Test]
    public async Task Convert_NonTagInput_ReturnsTransparent()
    {
        var converter = BuildConverter();

        var result = converter.Convert("not-a-tag", typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsEqualTo(Brushes.Transparent);
    }

    /// <summary>
    ///     When the styled property for a tag is unbound, the converter falls
    ///     back to transparent instead of throwing or returning a hard-coded
    ///     brush.
    /// </summary>
    [Test]
    public async Task Convert_UnboundBrushProperty_ReturnsTransparent()
    {
        var converter = new TrafficFlowColorTagToBrushConverter();

        var result = converter.Convert(TrafficFlowColorTag.Red, typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsEqualTo(Brushes.Transparent);
    }

    /// <summary>
    ///     ConvertBack is unsupported and throws.
    /// </summary>
    [Test]
    public async Task ConvertBack_AnyInput_Throws()
    {
        var converter = BuildConverter();

        await Assert.That(() => converter.ConvertBack(Brushes.Red, typeof(TrafficFlowColorTag), null, CultureInfo.InvariantCulture))
            .Throws<System.NotSupportedException>();
    }

    private static TrafficFlowColorTagToBrushConverter BuildConverter()
    {
        // Arbitrary distinct placeholder brushes — the converter is being tested,
        // not the production palette. The real palette lives in App.axaml under
        // ThemeDictionaries and is what end users see.
        var converter = new TrafficFlowColorTagToBrushConverter
        {
            RedBrush = new SolidColorBrush(Color.FromRgb(1, 1, 1)),
            OrangeBrush = new SolidColorBrush(Color.FromRgb(2, 2, 2)),
            YellowBrush = new SolidColorBrush(Color.FromRgb(3, 3, 3)),
            GreenBrush = new SolidColorBrush(Color.FromRgb(4, 4, 4)),
            BlueBrush = new SolidColorBrush(Color.FromRgb(5, 5, 5)),
            PurpleBrush = new SolidColorBrush(Color.FromRgb(6, 6, 6)),
            GrayBrush = new SolidColorBrush(Color.FromRgb(7, 7, 7)),
        };
        return converter;
    }

    private static IBrush? GetExpectedBrush(TrafficFlowColorTagToBrushConverter converter, TrafficFlowColorTag tag)
    {
        return tag switch
        {
            TrafficFlowColorTag.Red => converter.RedBrush,
            TrafficFlowColorTag.Orange => converter.OrangeBrush,
            TrafficFlowColorTag.Yellow => converter.YellowBrush,
            TrafficFlowColorTag.Green => converter.GreenBrush,
            TrafficFlowColorTag.Blue => converter.BlueBrush,
            TrafficFlowColorTag.Purple => converter.PurpleBrush,
            TrafficFlowColorTag.Gray => converter.GrayBrush,
            _ => null,
        };
    }
}
