using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Stores traffic flows in a bounded in-memory ring buffer.
/// </summary>
public sealed class TrafficStore : ITrafficStore
{
    private const int DefaultCapacity = 10000;
    private const int DefaultLargeBodyThresholdBytes = 1024 * 1024;
    private const int DefaultMaxBodyBytes = 100 * 1024 * 1024;
    private const int HardMemoryPressurePercent = 90;
    private readonly ConcurrentDictionary<Guid, TrafficFlow> _flows;
    private readonly int _largeBodyThresholdBytes;
    private readonly int _maxBodyBytes;
    private readonly TrafficStoreMemoryUsageProvider _memoryUsageProvider;
    private readonly Guid[] _order;
    private readonly string _spillDirectoryPath;
    private readonly object _syncRoot;
    private int _count;
    private int _nextIndex;

    /// <summary>
    ///     Initializes a new <see cref="TrafficStore" /> instance with the default capacity.
    /// </summary>
    public TrafficStore()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficStore" /> instance with the specified capacity.
    /// </summary>
    /// <param name="capacity">
    ///     The maximum number of flows to retain.
    /// </param>
    public TrafficStore(int capacity)
        : this(
            capacity,
            DefaultLargeBodyThresholdBytes,
            DefaultMaxBodyBytes,
            TrafficStoreDefaults.GetMemoryUsageRatio,
            TrafficStoreDefaults.GetSpillDirectoryPath())
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficStore" /> instance with explicit body-retention
    ///     safeguards and memory-pressure probing.
    /// </summary>
    /// <param name="capacity">The maximum number of flows to retain.</param>
    /// <param name="largeBodyThresholdBytes">Body size above which captures spill to disk.</param>
    /// <param name="maxBodyBytes">Absolute cap for captured body bytes.</param>
    /// <param name="memoryUsageProvider">Supplies current process memory pressure ratio.</param>
    /// <param name="spillDirectoryPath">Directory where spilled body files are written.</param>
    public TrafficStore(
        int capacity,
        int largeBodyThresholdBytes,
        int maxBodyBytes,
        TrafficStoreMemoryUsageProvider? memoryUsageProvider,
        string? spillDirectoryPath)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(largeBodyThresholdBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBodyBytes);
        if (largeBodyThresholdBytes > maxBodyBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(largeBodyThresholdBytes), "Large-body threshold cannot exceed max body bytes.");
        }
        Capacity = capacity;
        _largeBodyThresholdBytes = largeBodyThresholdBytes;
        _maxBodyBytes = maxBodyBytes;
        _memoryUsageProvider = memoryUsageProvider ?? TrafficStoreDefaults.GetMemoryUsageRatio;
        _spillDirectoryPath = string.IsNullOrWhiteSpace(spillDirectoryPath) ? TrafficStoreDefaults.GetSpillDirectoryPath() : spillDirectoryPath;
        _count = 0;
        _nextIndex = 0;

        var flows = new ConcurrentDictionary<Guid, TrafficFlow>();
        var order = new Guid[capacity];
        var syncRoot = new object();
        _flows = flows;
        _order = order;
        _syncRoot = syncRoot;
    }

    /// <summary>
    ///     Adds a traffic flow to the store.
    /// </summary>
    /// <param name="flow">
    ///     The flow to store.
    /// </param>
    public void Add(TrafficFlow flow)
    {
        lock (_syncRoot)
        {
            if (_flows.TryGetValue(flow.Id, out var existingFlow))
            {
                CleanupFlowSpillFiles(existingFlow);
                ApplyBodyRetentionPolicy(flow);
                _flows[flow.Id] = flow;
                return;
            }

            ApplyHardMemoryPressureEvictionWhenNeeded();
            ApplyBodyRetentionPolicy(flow);
            RemoveOldestFlowWhenFull();
            _flows[flow.Id] = flow;
            _order[_nextIndex] = flow.Id;
            _nextIndex = GetNextIndex(_nextIndex);
        }
    }

    /// <summary>
    ///     Gets the configured flow capacity.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    ///     Removes all stored flows.
    /// </summary>
    public void Clear()
    {
        lock (_syncRoot)
        {
            CleanupAllSpillFiles();
            Array.Clear(_order, 0, _order.Length);
            _flows.Clear();
            _count = 0;
            _nextIndex = 0;
        }
    }

    /// <summary>
    ///     Gets the current number of stored flows.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _count;
            }
        }
    }

    /// <summary>
    ///     Returns all stored flows ordered from newest to oldest.
    /// </summary>
    /// <returns>
    ///     A snapshot of the currently stored flows.
    /// </returns>
    public IReadOnlyList<TrafficFlow> GetAll()
    {
        lock (_syncRoot)
        {
            return CreateSnapshot();
        }
    }

    /// <summary>
    ///     Looks up a stored flow by identifier.
    /// </summary>
    /// <param name="id">
    ///     The flow identifier.
    /// </param>
    /// <returns>
    ///     The stored flow when found; otherwise, <see langword="null" />.
    /// </returns>
    public TrafficFlow? GetById(Guid id)
    {
        if (_flows.TryGetValue(id, out TrafficFlow? flow))
        {
            return flow;
        }

        return null;
    }

    private void ApplyBodyRetentionPolicy(TrafficFlow flow)
    {
        ApplyRequestBodyRetentionPolicy(flow);
        ApplyResponseBodyRetentionPolicy(flow);
    }

    private void ApplyHardMemoryPressureEvictionWhenNeeded()
    {
        if (_count == 0)
        {
            return;
        }

        var usagePercent = _memoryUsageProvider() * 100D;
        if (usagePercent <= HardMemoryPressurePercent)
        {
            return;
        }

        var evictionCount = Math.Max(1, _count / 10);
        EvictOldestFlows(evictionCount);
    }

    private void ApplyRequestBodyRetentionPolicy(TrafficFlow flow)
    {
        if (flow.Request is null)
        {
            return;
        }

        var retainedBody = CreateRetainedBody(flow.Request.Body);
        if (!retainedBody.IsChanged)
        {
            return;
        }

        flow.UpdateRequestBodyForStorage(retainedBody.Body, retainedBody.SpillFilePath, retainedBody.IsTruncated);
    }

    private void ApplyResponseBodyRetentionPolicy(TrafficFlow flow)
    {
        if (flow.Response is null)
        {
            return;
        }

        var retainedBody = CreateRetainedBody(flow.Response.Body);
        if (!retainedBody.IsChanged)
        {
            return;
        }

        flow.UpdateResponseBodyForStorage(retainedBody.Body, retainedBody.SpillFilePath, retainedBody.IsTruncated);
    }

    private void CleanupAllSpillFiles()
    {
        foreach (var flow in _flows.Values)
        {
            CleanupFlowSpillFiles(flow);
        }
    }

    private void CleanupFlowSpillFiles(TrafficFlow flow)
    {
        DeleteSpillFile(flow.RequestBodySpillFilePath);
        DeleteSpillFile(flow.ResponseBodySpillFilePath);
    }

    private RetainedBody CreateRetainedBody(ReadOnlyMemory<byte> body)
    {
        var retainedBytes = body;
        var isTruncated = false;
        var spillFilePath = default(string);
        if (retainedBytes.Length > _maxBodyBytes)
        {
            retainedBytes = retainedBytes[.._maxBodyBytes];
            isTruncated = true;
        }

        if (retainedBytes.Length > _largeBodyThresholdBytes)
        {
            spillFilePath = TrySpillToDisk(retainedBytes);
            if (spillFilePath is not null)
            {
                retainedBytes = ReadOnlyMemory<byte>.Empty;
            }
        }

        var hasChanged = isTruncated || spillFilePath is not null;
        var retainedBody = new RetainedBody(retainedBytes, spillFilePath, isTruncated, hasChanged);
        return retainedBody;
    }

    private List<TrafficFlow> CreateSnapshot()
    {
        var flows = new List<TrafficFlow>(_count);

        if (_count == 0)
        {
            return flows;
        }

        var index = GetNewestIndex();

        for (var itemIndex = 0; itemIndex < _count; itemIndex++)
        {
            var flowIdentifier = _order[index];

            if (_flows.TryGetValue(flowIdentifier, out TrafficFlow? flow))
            {
                flows.Add(flow);
            }

            index = GetPreviousIndex(index);
        }

        return flows;
    }

    private void DeleteSpillFile(string? spillFilePath)
    {
        if (string.IsNullOrWhiteSpace(spillFilePath))
        {
            return;
        }

        if (!File.Exists(spillFilePath))
        {
            return;
        }

        try
        {
            File.Delete(spillFilePath);
        }
        catch (IOException exception)
        {
            _ = exception;
        }
        catch (UnauthorizedAccessException exception)
        {
            _ = exception;
        }
    }

    private void EvictOldestFlows(int evictionCount)
    {
        if (evictionCount <= 0)
        {
            return;
        }

        var ordered = GetOrderedFlowIdentifiersOldestToNewest();
        var removeCount = Math.Min(evictionCount, ordered.Count);
        for (var index = 0; index < removeCount; index++)
        {
            var flowIdentifier = ordered[index];
            if (_flows.TryRemove(flowIdentifier, out var removedFlow))
            {
                CleanupFlowSpillFiles(removedFlow);
            }
        }

        RebuildOrderFromOldestToNewest(ordered, removeCount);
    }

    private int GetNewestIndex()
    {
        if (_nextIndex == 0)
        {
            return Capacity - 1;
        }

        return _nextIndex - 1;
    }

    private int GetNextIndex(int index)
    {
        if (index == Capacity - 1)
        {
            return 0;
        }

        return index + 1;
    }

    private List<Guid> GetOrderedFlowIdentifiersOldestToNewest()
    {
        var identifiers = new List<Guid>(_count);
        if (_count == 0)
        {
            return identifiers;
        }

        var startIndex = _count == Capacity ? _nextIndex : 0;
        for (var index = 0; index < _count; index++)
        {
            var position = (startIndex + index) % Capacity;
            identifiers.Add(_order[position]);
        }

        return identifiers;
    }

    private int GetPreviousIndex(int index)
    {
        if (index == 0)
        {
            return Capacity - 1;
        }

        return index - 1;
    }

    private void RebuildOrderFromOldestToNewest(List<Guid> previousOrder, int removedCount)
    {
        Array.Clear(_order, 0, _order.Length);
        var retainedCount = previousOrder.Count - removedCount;
        for (var index = 0; index < retainedCount; index++)
        {
            _order[index] = previousOrder[index + removedCount];
        }

        _count = retainedCount;
        _nextIndex = retainedCount;
    }

    private void RemoveOldestFlowWhenFull()
    {
        if (_count == Capacity)
        {
            var flowIdentifier = _order[_nextIndex];
            if (_flows.TryRemove(flowIdentifier, out var removedFlow))
            {
                CleanupFlowSpillFiles(removedFlow);
            }
            return;
        }

        _count++;
    }

    private string? TrySpillToDisk(ReadOnlyMemory<byte> body)
    {
        try
        {
            Directory.CreateDirectory(_spillDirectoryPath);
            var spillFileName = Guid.NewGuid().ToString("N") + ".body";
            var spillPath = Path.Combine(_spillDirectoryPath, spillFileName);
            File.WriteAllBytes(spillPath, body.ToArray());
            return spillPath;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private readonly struct RetainedBody
    {
        public ReadOnlyMemory<byte> Body { get; }

        public bool IsChanged { get; }

        public bool IsTruncated { get; }

        public string? SpillFilePath { get; }

        public RetainedBody(ReadOnlyMemory<byte> body, string? spillFilePath, bool isTruncated, bool isChanged)
        {
            Body = body;
            SpillFilePath = spillFilePath;
            IsTruncated = isTruncated;
            IsChanged = isChanged;
        }
    }
}