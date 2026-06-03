using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="WebSocketStore" />.
/// </summary>
[NotInParallel]
public sealed class WebSocketStoreTests
{
    /// <summary>
    ///     Verifies that <see cref="WebSocketStore.Add" /> increases the stored flow count.
    /// </summary>
    [Test]
    public async Task Add_WhenFlowStored_IncreasesCount()
    {
        var store = new WebSocketStore(3);
        var flow = CreateFlow();

        store.Add(flow);

        await Assert.That(store.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that adding beyond capacity evicts the oldest flow.
    /// </summary>
    [Test]
    public async Task Add_WhenStoreIsFull_EvictsOldestFlow()
    {
        var store = new WebSocketStore(2);
        var firstFlow = CreateFlow();
        var secondFlow = CreateFlow();
        var thirdFlow = CreateFlow();
        store.Add(firstFlow);
        store.Add(secondFlow);

        store.Add(thirdFlow);

        await Assert.That(store.Count).IsEqualTo(2);
        await Assert.That(store.GetById(firstFlow.Id)).IsNull();
        await Assert.That(store.GetById(thirdFlow.Id)).IsNotNull();
    }

    /// <summary>
    ///     Verifies that the parameterless constructor selects a positive default capacity.
    /// </summary>
    [Test]
    public async Task Constructor_WhenDefault_UsesPositiveCapacity()
    {
        var store = new WebSocketStore();

        await Assert.That(store.Capacity).IsGreaterThan(0);
    }

    /// <summary>
    ///     Verifies that a non-positive capacity is rejected.
    /// </summary>
    [Test]
    public async Task Constructor_WhenCapacityIsZero_Throws()
    {
        await Assert.That(() => new WebSocketStore(0)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketStore.Clear" /> removes all stored flows.
    /// </summary>
    [Test]
    public async Task Clear_WhenFlowsExist_RemovesAllFlows()
    {
        var store = new WebSocketStore(2);
        store.Add(CreateFlow());
        store.Add(CreateFlow());

        store.Clear();

        await Assert.That(store.Count).IsEqualTo(0);
        await Assert.That(store.GetAll().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketStore.GetAll" /> returns newest items first.
    /// </summary>
    [Test]
    public async Task GetAll_WhenFlowsExist_ReturnsNewestFirst()
    {
        var store = new WebSocketStore(3);
        var firstFlow = CreateFlow();
        var secondFlow = CreateFlow();
        var thirdFlow = CreateFlow();
        store.Add(firstFlow);
        store.Add(secondFlow);
        store.Add(thirdFlow);

        IReadOnlyList<WebSocketFlow> flows = store.GetAll();

        await Assert.That(flows[0].Id).IsEqualTo(thirdFlow.Id);
        await Assert.That(flows[1].Id).IsEqualTo(secondFlow.Id);
        await Assert.That(flows[2].Id).IsEqualTo(firstFlow.Id);
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketStore.GetById" /> returns the stored flow.
    /// </summary>
    [Test]
    public async Task GetById_WhenFlowExists_ReturnsFlow()
    {
        var store = new WebSocketStore(2);
        var flow = CreateFlow();
        store.Add(flow);

        var storedFlow = store.GetById(flow.Id);

        await Assert.That(storedFlow).IsEqualTo(flow);
    }

    /// <summary>
    ///     Verifies that re-adding an existing flow id replaces the entry in place.
    /// </summary>
    [Test]
    public async Task Add_WhenSameIdAddedTwice_ReplacesInPlaceWithoutDuplicating()
    {
        var store = new WebSocketStore(3);
        var id = Guid.NewGuid();
        var originalFlow = CreateFlow(id, "127.0.0.1:8080");
        var replacementFlow = CreateFlow(id, "127.0.0.1:9090");
        store.Add(originalFlow);

        store.Add(replacementFlow);

        await Assert.That(store.Count).IsEqualTo(1);
        await Assert.That(store.GetAll().Count).IsEqualTo(1);
        await Assert.That(store.GetById(id)).IsEqualTo(replacementFlow);
    }

    /// <summary>
    ///     Verifies that re-adding an existing flow id does not consume ring-buffer capacity.
    /// </summary>
    [Test]
    public async Task Add_WhenSameIdAddedTwiceThenFilledToCapacity_DoesNotEvictOldestFlow()
    {
        var store = new WebSocketStore(2);
        var firstFlow = CreateFlow();
        var secondFlow = CreateFlow();
        store.Add(firstFlow);
        store.Add(firstFlow);

        store.Add(secondFlow);

        await Assert.That(store.Count).IsEqualTo(2);
        await Assert.That(store.GetById(firstFlow.Id)).IsNotNull();
        await Assert.That(store.GetById(secondFlow.Id)).IsNotNull();
        await Assert.That(store.GetAll().Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketStore.GetById" /> returns null when missing.
    /// </summary>
    [Test]
    public async Task GetById_WhenFlowMissing_ReturnsNull()
    {
        var store = new WebSocketStore(2);

        var storedFlow = store.GetById(Guid.NewGuid());

        await Assert.That(storedFlow).IsNull();
    }

    /// <summary>
    ///     Verifies that concurrent reads during writes remain consistent.
    /// </summary>
    [Test]
    public async Task Access_WhenReadAndWriteConcurrently_RemainsConsistent()
    {
        var store = new WebSocketStore(32);
        var startSignal = new ManualResetEventSlim(false);
        var writer = Task.Run(() => WriteFlows(store, startSignal, 100));
        var readerOne = Task.Run(() => ReadFlows(store, startSignal, 100));
        var readerTwo = Task.Run(() => ReadFlows(store, startSignal, 100));

        startSignal.Set();
        await Task.WhenAll(writer, readerOne, readerTwo);

        await Assert.That(store.Count).IsEqualTo(32);
        await Assert.That(store.GetAll().Count).IsEqualTo(32);
    }

    private WebSocketFlow CreateFlow()
    {
        return CreateFlow(Guid.NewGuid(), "127.0.0.1:8080");
    }

    private WebSocketFlow CreateFlow(Guid id, string endpoint)
    {
        var traffic = new TrafficFlow(id, endpoint, DateTimeOffset.UtcNow);
        var flow = new WebSocketFlow(traffic);
        return flow;
    }

    private void ReadFlows(WebSocketStore store, ManualResetEventSlim startSignal, int iterations)
    {
        startSignal.Wait();

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            store.GetAll();
            _ = store.Count;
        }
    }

    private void WriteFlows(WebSocketStore store, ManualResetEventSlim startSignal, int iterations)
    {
        startSignal.Wait();

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var flow = CreateFlow();
            store.Add(flow);
        }
    }
}
