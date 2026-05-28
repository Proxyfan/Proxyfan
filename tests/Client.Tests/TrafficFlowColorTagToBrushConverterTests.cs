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
    ///     Each defined color tag returns a non-transparent brush instance.
    /// </summary>
    [Test]
    [Arguments(TrafficFlowColorTag.Red)]
    [Arguments(TrafficFlowColorTag.Orange)]
    [Arguments(TrafficFlowColorTag.Yellow)]
    [Arguments(TrafficFlowColorTag.Green)]
    [Arguments(TrafficFlowColorTag.Blue)]
    [Arguments(TrafficFlowColorTag.Purple)]
    [Arguments(TrafficFlowColorTag.Gray)]
    public async Task Convert_KnownTag_ReturnsNonTransparentBrush(TrafficFlowColorTag tag)
    {
        var converter = TrafficFlowColorTagToBrushConverter.Instance;

        var result = converter.Convert(tag, typeof(IBrush), null, CultureInfo.InvariantCulture);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEqualTo(Brushes.Transparent);
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
