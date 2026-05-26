using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="TrafficStore" />.
/// </summary>
[NotInParallel]
public sealed class TrafficStoreTests
{
    /// <summary>
    ///     Verifies that concurrent reads during writes remain consistent.
    /// </summary>
    [Test]
    public async Task Access_WhenReadAndWriteConcurrently_RemainsConsistent()
    {
        var store = new TrafficStore(32);
        var startSignal = new ManualResetEventSlim(false);
        var writer = Task.Run(() => WriteFlows(store, startSignal, 100));
        var readerOne = Task.Run(() => ReadFlows(store, startSignal, 100));
        var readerTwo = Task.Run(() => ReadFlows(store, startSignal, 100));

        startSignal.Set();
        await Task.WhenAll(writer, readerOne, readerTwo);

        await Assert.That(store.Count).IsEqualTo(32);
        await Assert.That(store.GetAll().Count).IsEqualTo(32);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficStore.Add" /> increases the stored flow count.
    /// </summary>
    [Test]
    public async Task Add_WhenFlowStored_IncreasesCount()
    {
        var store = new TrafficStore(3);
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
        var store = new TrafficStore(2);
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
    ///     Verifies that <see cref="TrafficStore.Clear" /> removes all stored flows.
    /// </summary>
    [Test]
    public async Task Clear_WhenFlowsExist_RemovesAllFlows()
    {
        var store = new TrafficStore(2);
        store.Add(CreateFlow());
        store.Add(CreateFlow());

        store.Clear();

        await Assert.That(store.Count).IsEqualTo(0);
        await Assert.That(store.GetAll().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficStore.Count" /> reflects the stored items.
    /// </summary>
    [Test]
    public async Task Count_WhenFlowsAdded_MatchesStoredItems()
    {
        var store = new TrafficStore(4);
        store.Add(CreateFlow());
        store.Add(CreateFlow());
        store.Add(CreateFlow());

        await Assert.That(store.Count).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficStore.GetAll" /> returns newest items first.
    /// </summary>
    [Test]
    public async Task GetAll_WhenFlowsExist_ReturnsNewestFirst()
    {
        var store = new TrafficStore(3);
        var firstFlow = CreateFlow();
        var secondFlow = CreateFlow();
        var thirdFlow = CreateFlow();
        store.Add(firstFlow);
        store.Add(secondFlow);
        store.Add(thirdFlow);

        IReadOnlyList<TrafficFlow> flows = store.GetAll();

        await Assert.That(flows[0].Id).IsEqualTo(thirdFlow.Id);
        await Assert.That(flows[1].Id).IsEqualTo(secondFlow.Id);
        await Assert.That(flows[2].Id).IsEqualTo(firstFlow.Id);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficStore.GetById" /> returns a stored flow.
    /// </summary>
    [Test]
    public async Task GetById_WhenFlowExists_ReturnsFlow()
    {
        var store = new TrafficStore(2);
        var flow = CreateFlow();
        store.Add(flow);

        var storedFlow = store.GetById(flow.Id);

        await Assert.That(storedFlow).IsEqualTo(flow);
    }

    private TrafficFlow CreateFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:8080", DateTimeOffset.UtcNow);
        return flow;
    }

    private void ReadFlows(TrafficStore store, ManualResetEventSlim startSignal, int iterations)
    {
        startSignal.Wait();

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            store.GetAll();
            _ = store.Count;
        }
    }

    private void WriteFlows(TrafficStore store, ManualResetEventSlim startSignal, int iterations)
    {
        startSignal.Wait();

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var flow = CreateFlow();
            store.Add(flow);
        }
    }
}