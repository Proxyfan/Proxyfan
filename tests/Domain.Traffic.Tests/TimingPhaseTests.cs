using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="TimingPhase" />.
/// </summary>
public sealed class TimingPhaseTests
{
    /// <summary>
    ///     Verifies that the constructor records all four data points verbatim.
    /// </summary>
    [Test]
    public async Task Constructor_AssignsAllProperties_StoresValues()
    {
        var phase = new TimingPhase("Request", 0.25, 0.5, 123.4);

        await Assert.That(phase.Name).IsEqualTo("Request");
        await Assert.That(phase.StartFraction).IsEqualTo(0.25);
        await Assert.That(phase.WidthFraction).IsEqualTo(0.5);
        await Assert.That(phase.DurationMilliseconds).IsEqualTo(123.4);
    }

    /// <summary>
    ///     Verifies that two phases with identical values are equal under record semantics.
    /// </summary>
    [Test]
    public async Task Equals_TwoPhasesWithSameValues_AreEqual()
    {
        var left = new TimingPhase("Waiting", 0.1, 0.2, 50d);
        var right = new TimingPhase("Waiting", 0.1, 0.2, 50d);

        await Assert.That(left).IsEqualTo(right);
    }
}
