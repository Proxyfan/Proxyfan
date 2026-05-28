using System.Threading.Tasks;
using Proxyfan.Client.Inspector.ViewModels;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="TimingPhaseDurationFormatter" />.
/// </summary>
public sealed class TimingPhaseDurationFormatterTests
{
    /// <summary>
    ///     Verifies that whole milliseconds format with two trailing zeros.
    /// </summary>
    [Test]
    public async Task Format_WholeNumber_ProducesTwoDecimalZeros()
    {
        var result = TimingPhaseDurationFormatter.Format(100d);

        await Assert.That(result).IsEqualTo("100.00 ms");
    }

    /// <summary>
    ///     Verifies that fractional milliseconds are rounded to two decimal places.
    /// </summary>
    [Test]
    public async Task Format_FractionalMilliseconds_RoundsToTwoDecimals()
    {
        var result = TimingPhaseDurationFormatter.Format(42.567);

        await Assert.That(result).IsEqualTo("42.57 ms");
    }

    /// <summary>
    ///     Verifies that zero milliseconds yields <c>"0.00 ms"</c>.
    /// </summary>
    [Test]
    public async Task Format_Zero_ProducesZeroLabel()
    {
        var result = TimingPhaseDurationFormatter.Format(0d);

        await Assert.That(result).IsEqualTo("0.00 ms");
    }
}
