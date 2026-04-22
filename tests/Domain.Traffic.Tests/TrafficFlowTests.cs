using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>Tests for <see cref="TrafficFlow" />.</summary>
internal sealed class TrafficFlowTests
{
    private static TrafficFlow CreateFlow()
    {
        return new TrafficFlow(Guid.NewGuid(), "127.0.0.1:12345", DateTimeOffset.UtcNow);
    }

    /// <summary>Verifies that a newly created flow starts in the <see cref="TrafficFlowStatus.Pending" /> state.</summary>
    [Test]
    public async Task Constructor_NewFlow_StatusIsPending()
    {
        var flow = CreateFlow();
        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Pending);
    }

    /// <summary>Verifies that a newly created flow has no <see cref="TrafficFlow.FailedAt" /> timestamp.</summary>
    [Test]
    public async Task Constructor_NewFlow_FailedAtIsNull()
    {
        var flow = CreateFlow();
        await Assert.That(flow.FailedAt).IsNull();
    }

    /// <summary>Verifies that <see cref="TrafficFlow.Fail" /> transitions a <c>Pending</c> flow to <c>Failed</c>.</summary>
    [Test]
    public async Task Fail_WhenPending_SetsStatusToFailed()
    {
        var flow = CreateFlow();

        flow.Fail();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>Verifies that <see cref="TrafficFlow.Fail" /> sets <see cref="TrafficFlow.FailedAt" /> to approximately now.</summary>
    [Test]
    public async Task Fail_WhenPending_SetsFailedAtToApproximatelyNow()
    {
        var before = DateTimeOffset.UtcNow;
        var flow = CreateFlow();

        flow.Fail();

        var after = DateTimeOffset.UtcNow;
        await Assert.That(flow.FailedAt).IsNotNull();
        await Assert.That(flow.FailedAt!.Value >= before).IsTrue();
        await Assert.That(flow.FailedAt!.Value <= after).IsTrue();
    }

    /// <summary>
    ///     Verifies that calling <see cref="TrafficFlow.Fail" /> on an already-failed flow is a no-op
    ///     (the <see cref="TrafficFlow.FailedAt" /> timestamp is not overwritten).
    /// </summary>
    [Test]
    public async Task Fail_WhenAlreadyFailed_IsNoOp()
    {
        var flow = CreateFlow();
        flow.Fail();
        var firstFailedAt = flow.FailedAt;

        flow.Fail();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Failed);
        await Assert.That(flow.FailedAt).IsEqualTo(firstFailedAt);
    }

    /// <summary>Verifies that <see cref="TrafficFlow.Id" /> is preserved from construction.</summary>
    [Test]
    public async Task Constructor_ProvidedId_IsPreserved()
    {
        var id = Guid.NewGuid();
        var flow = new TrafficFlow(id, "10.0.0.1:9999", DateTimeOffset.UtcNow);

        await Assert.That(flow.Id).IsEqualTo(id);
    }

    /// <summary>Verifies that <see cref="TrafficFlow.ClientEndPoint" /> is preserved from construction.</summary>
    [Test]
    public async Task Constructor_ProvidedClientEndPoint_IsPreserved()
    {
        const string endPoint = "192.168.1.1:54321";
        var flow = new TrafficFlow(Guid.NewGuid(), endPoint, DateTimeOffset.UtcNow);

        await Assert.That(flow.ClientEndPoint).IsEqualTo(endPoint);
    }
}
