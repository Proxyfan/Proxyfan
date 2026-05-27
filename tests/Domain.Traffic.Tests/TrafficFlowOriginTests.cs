using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for the <see cref="TrafficFlowOrigin" /> annotation attached to
///     <see cref="TrafficFlow" />, ensuring the live-capture default is preserved and the
///     explicit-origin constructor records the supplied value.
/// </summary>
public sealed class TrafficFlowOriginTests
{
    /// <summary>
    ///     Verifies that the legacy three-argument constructor defaults to
    ///     <see cref="TrafficFlowOrigin.Captured" /> for backward compatibility.
    /// </summary>
    [Test]
    public async Task Constructor_LegacyOverload_DefaultsToCaptured()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:80", DateTimeOffset.UtcNow);

        await Assert.That(flow.Origin).IsEqualTo(TrafficFlowOrigin.Captured);
    }

    /// <summary>
    ///     Verifies that the explicit-origin constructor records
    ///     <see cref="TrafficFlowOrigin.Repeated" />.
    /// </summary>
    [Test]
    public async Task Constructor_RepeatedOrigin_RecordsRepeated()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "(repeat)", DateTimeOffset.UtcNow, TrafficFlowOrigin.Repeated);

        await Assert.That(flow.Origin).IsEqualTo(TrafficFlowOrigin.Repeated);
    }

    /// <summary>
    ///     Verifies that the explicit-origin constructor records
    ///     <see cref="TrafficFlowOrigin.Composed" />.
    /// </summary>
    [Test]
    public async Task Constructor_ComposedOrigin_RecordsComposed()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "(composer)", DateTimeOffset.UtcNow, TrafficFlowOrigin.Composed);

        await Assert.That(flow.Origin).IsEqualTo(TrafficFlowOrigin.Composed);
    }
}
