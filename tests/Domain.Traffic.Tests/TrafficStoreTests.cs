using System;
using System.Collections.Generic;
using System.IO;
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
    ///     Verifies that responses above the large-body threshold spill to disk.
    /// </summary>
    [Test]
    public async Task Add_ResponseBodyExceedsThreshold_SpillsToDisk()
    {
        var spillDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var store = new TrafficStore(4, 8, 128, GetLowMemoryUsageRatio, spillDirectoryPath);
            var body = new byte[16];
            var flow = CreateCompletedFlowWithResponseBody(body);

            store.Add(flow);

            var stored = store.GetById(flow.Id);
            await Assert.That(stored).IsNotNull();
            await Assert.That(stored!.ResponseBodySpillFilePath).IsNotNull();
            await Assert.That(stored.Response!.Body.Length).IsEqualTo(0);
            await Assert.That(File.Exists(stored.ResponseBodySpillFilePath!)).IsTrue();
            var persistedBytes = await File.ReadAllBytesAsync(stored.ResponseBodySpillFilePath!);
            await Assert.That(persistedBytes).IsEquivalentTo(body);

            store.Clear();
            await Assert.That(File.Exists(stored.ResponseBodySpillFilePath!)).IsFalse();
        }
        finally
        {
            DeleteDirectoryIfExists(spillDirectoryPath);
        }
    }

    /// <summary>
    ///     Verifies that responses larger than the configured cap are truncated.
    /// </summary>
    [Test]
    public async Task Add_ResponseBodyExceedsMaxBytes_Truncates()
    {
        var spillDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var store = new TrafficStore(4, 8, 12, GetLowMemoryUsageRatio, spillDirectoryPath);
            var body = new byte[20];
            var flow = CreateCompletedFlowWithResponseBody(body);

            store.Add(flow);

            var stored = store.GetById(flow.Id);
            await Assert.That(stored).IsNotNull();
            await Assert.That(stored!.IsResponseBodyTruncated).IsTrue();
            await Assert.That(stored.ResponseBodySpillFilePath).IsNotNull();
            var persistedBytes = await File.ReadAllBytesAsync(stored.ResponseBodySpillFilePath!);
            await Assert.That(persistedBytes.Length).IsEqualTo(12);
        }
        finally
        {
            DeleteDirectoryIfExists(spillDirectoryPath);
        }
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

    /// <summary>
    ///     Verifies that re-adding an existing flow id replaces the entry in place.
    /// </summary>
    [Test]
    public async Task Add_WhenSameIdAddedTwice_ReplacesInPlaceWithoutDuplicating()
    {
        var store = new TrafficStore(3);
        var id = Guid.NewGuid();
        var originalFlow = new TrafficFlow(id, "127.0.0.1:8080", DateTimeOffset.UtcNow);
        var replacementFlow = new TrafficFlow(id, "127.0.0.1:9090", DateTimeOffset.UtcNow);
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
        var store = new TrafficStore(2);
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
    ///     Verifies that high memory pressure evicts the oldest ten percent of flows.
    /// </summary>
    [Test]
    public async Task Add_MemoryPressureHigh_EvictsOldestTenPercent()
    {
        var memoryUsageRatio = 0.0D;
        var spillDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var store = new TrafficStore(10, 1024, 1024, () => memoryUsageRatio, spillDirectoryPath);
            var firstFlow = CreateFlow();
            for (var index = 0; index < 10; index++)
            {
                var flow = index == 0 ? firstFlow : CreateFlow();
                store.Add(flow);
            }

            memoryUsageRatio = 0.95D;
            var eleventhFlow = CreateFlow();
            store.Add(eleventhFlow);

            await Assert.That(store.Count).IsEqualTo(10);
            await Assert.That(store.GetById(firstFlow.Id)).IsNull();
            await Assert.That(store.GetById(eleventhFlow.Id)).IsNotNull();
        }
        finally
        {
            DeleteDirectoryIfExists(spillDirectoryPath);
        }
    }

    private TrafficFlow CreateCompletedFlowWithResponseBody(byte[] responseBody)
    {
        var flow = CreateFlow();
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = responseBody,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        flow.SetResponse(new HypertextTransferProtocolResponseData(responseParameters));
        flow.Complete();
        return flow;
    }

    private TrafficFlow CreateFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:8080", DateTimeOffset.UtcNow);
        return flow;
    }

    private void DeleteDirectoryIfExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        Directory.Delete(directoryPath, true);
    }

    private double GetLowMemoryUsageRatio()
    {
        return 0.1D;
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