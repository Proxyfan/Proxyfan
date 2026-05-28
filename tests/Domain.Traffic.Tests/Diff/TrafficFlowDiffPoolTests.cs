using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic.Diff;

namespace Proxyfan.Domain.Traffic.Tests.Diff;

/// <summary>
///     Tests for <see cref="TrafficFlowDiffPool" />.
/// </summary>
public sealed class TrafficFlowDiffPoolTests
{
    /// <summary>
    ///     Verifies that the default constructor sets capacity to DefaultCapacity.
    /// </summary>
    [Test]
    public async Task Constructor_Default_UsesDefaultCapacity()
    {
        var pool = new TrafficFlowDiffPool();

        await Assert.That(pool.Capacity).IsEqualTo(TrafficFlowDiffPool.DefaultCapacity);
        await Assert.That(pool.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the explicit-capacity constructor rejects non-positive capacities.
    /// </summary>
    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task Constructor_NonPositiveCapacity_Throws(int capacity)
    {
        await Assert.That(() => new TrafficFlowDiffPool(capacity))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that Add appends a flow and increments Count.
    /// </summary>
    [Test]
    public async Task Add_NewFlow_IncrementsCount()
    {
        var pool = new TrafficFlowDiffPool();
        var flow = BuildFlow();

        pool.Add(flow);

        await Assert.That(pool.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that Add silently ignores duplicate flows.
    /// </summary>
    [Test]
    public async Task Add_DuplicateFlow_IsIgnored()
    {
        var pool = new TrafficFlowDiffPool();
        var flow = BuildFlow();
        pool.Add(flow);

        pool.Add(flow);

        await Assert.That(pool.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that Add evicts the oldest flow when at capacity.
    /// </summary>
    [Test]
    public async Task Add_OverCapacity_EvictsOldest()
    {
        var pool = new TrafficFlowDiffPool(capacity: 2);
        var flowOne = BuildFlow();
        var flowTwo = BuildFlow();
        var flowThree = BuildFlow();
        pool.Add(flowOne);
        pool.Add(flowTwo);

        pool.Add(flowThree);

        await Assert.That(pool.Count).IsEqualTo(2);
        var snapshot = pool.Snapshot();
        await Assert.That(snapshot[0]).IsSameReferenceAs(flowTwo);
        await Assert.That(snapshot[1]).IsSameReferenceAs(flowThree);
    }

    /// <summary>
    ///     Verifies that Remove deletes a present flow and returns silently.
    /// </summary>
    [Test]
    public async Task Remove_PresentFlow_RemovesIt()
    {
        var pool = new TrafficFlowDiffPool();
        var flow = BuildFlow();
        pool.Add(flow);

        pool.Remove(flow);

        await Assert.That(pool.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that Remove on an absent flow is a no-op.
    /// </summary>
    [Test]
    public async Task Remove_AbsentFlow_IsNoop()
    {
        var pool = new TrafficFlowDiffPool();
        var flow = BuildFlow();

        pool.Remove(flow);

        await Assert.That(pool.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that Clear empties the pool.
    /// </summary>
    [Test]
    public async Task Clear_NonEmptyPool_RemovesAll()
    {
        var pool = new TrafficFlowDiffPool();
        pool.Add(BuildFlow());
        pool.Add(BuildFlow());

        pool.Clear();

        await Assert.That(pool.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that Clear on an empty pool does not raise Changed.
    /// </summary>
    [Test]
    public async Task Clear_EmptyPool_DoesNotRaiseEvent()
    {
        var pool = new TrafficFlowDiffPool();
        var raised = false;
        pool.Changed += _ => raised = true;

        pool.Clear();

        await Assert.That(raised).IsFalse();
    }

    /// <summary>
    ///     Verifies that Add raises Changed when adding a new flow.
    /// </summary>
    [Test]
    public async Task Add_NewFlow_RaisesChanged()
    {
        var pool = new TrafficFlowDiffPool();
        var raised = false;
        pool.Changed += _ => raised = true;

        pool.Add(BuildFlow());

        await Assert.That(raised).IsTrue();
    }

    /// <summary>
    ///     Verifies that Snapshot returns flows in insertion order.
    /// </summary>
    [Test]
    public async Task Snapshot_AfterAdds_ReturnsFlowsInOrder()
    {
        var pool = new TrafficFlowDiffPool();
        var flowOne = BuildFlow();
        var flowTwo = BuildFlow();
        pool.Add(flowOne);
        pool.Add(flowTwo);

        var snapshot = pool.Snapshot();

        await Assert.That(snapshot.Count).IsEqualTo(2);
        await Assert.That(snapshot[0]).IsSameReferenceAs(flowOne);
        await Assert.That(snapshot[1]).IsSameReferenceAs(flowTwo);
    }

    /// <summary>
    ///     Verifies that Clear on a non-empty pool raises Changed exactly once.
    /// </summary>
    [Test]
    public async Task Clear_NonEmptyPoolWithSubscriber_RaisesChanged()
    {
        var pool = new TrafficFlowDiffPool();
        pool.Add(BuildFlow());
        pool.Add(BuildFlow());
        var raisedCount = 0;
        pool.Changed += _ => raisedCount++;

        pool.Clear();

        await Assert.That(raisedCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that Remove on a present flow raises Changed once when a subscriber is attached.
    /// </summary>
    [Test]
    public async Task Remove_PresentFlowWithSubscriber_RaisesChanged()
    {
        var pool = new TrafficFlowDiffPool();
        var flow = BuildFlow();
        pool.Add(flow);
        var raisedCount = 0;
        pool.Changed += _ => raisedCount++;

        pool.Remove(flow);

        await Assert.That(raisedCount).IsEqualTo(1);
    }

    private static TrafficFlow BuildFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        return flow;
    }
}
