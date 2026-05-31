using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Stores Remote Procedure Call (gRPC) flows in a bounded in-memory ring buffer. Mirrors
///     <see cref="ServerSentEventsStore" /> in semantics but holds
///     <see cref="RemoteProcedureCallFlow" /> instances which append captured messages over
///     time once an HTTP/2 response is detected to be <c>application/grpc</c>.
/// </summary>
public sealed class RemoteProcedureCallStore : IRemoteProcedureCallStore
{
    private const int DefaultCapacity = 10000;
    private readonly ConcurrentDictionary<Guid, RemoteProcedureCallFlow> _flows;
    private readonly Guid[] _order;
    private readonly object _syncRoot;
    private int _count;
    private int _nextIndex;

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallStore" /> with the default capacity.
    /// </summary>
    public RemoteProcedureCallStore()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallStore" /> with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of gRPC flows to retain.</param>
    public RemoteProcedureCallStore(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        Capacity = capacity;
        _count = 0;
        _nextIndex = 0;

        var flows = new ConcurrentDictionary<Guid, RemoteProcedureCallFlow>();
        var order = new Guid[capacity];
        var syncRoot = new object();
        _flows = flows;
        _order = order;
        _syncRoot = syncRoot;
    }

    /// <summary>
    ///     Adds a gRPC flow to the store.
    /// </summary>
    /// <param name="flow">The flow to store.</param>
    public void Add(RemoteProcedureCallFlow flow)
    {
        lock (_syncRoot)
        {
            RemoveOldestFlowWhenFull();
            _flows[flow.Id] = flow;
            _order[_nextIndex] = flow.Id;
            _nextIndex = GetNextIndex(_nextIndex);
        }
    }

    /// <summary>
    ///     Gets the configured gRPC flow capacity.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    ///     Removes all stored gRPC flows.
    /// </summary>
    public void Clear()
    {
        lock (_syncRoot)
        {
            Array.Clear(_order, 0, _order.Length);
            _flows.Clear();
            _count = 0;
            _nextIndex = 0;
        }
    }

    /// <summary>
    ///     Gets the current number of stored gRPC flows.
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
    ///     Returns all stored gRPC flows ordered from newest to oldest.
    /// </summary>
    /// <returns>A snapshot of the currently stored gRPC flows.</returns>
    public IReadOnlyList<RemoteProcedureCallFlow> GetAll()
    {
        lock (_syncRoot)
        {
            return CreateSnapshot();
        }
    }

    /// <summary>
    ///     Looks up a stored gRPC flow by identifier.
    /// </summary>
    /// <param name="id">The flow identifier.</param>
    /// <returns>The stored gRPC flow when found; otherwise, <see langword="null" />.</returns>
    public RemoteProcedureCallFlow? GetById(Guid id)
    {
        if (_flows.TryGetValue(id, out RemoteProcedureCallFlow? flow))
        {
            return flow;
        }

        return null;
    }

    private List<RemoteProcedureCallFlow> CreateSnapshot()
    {
        var flows = new List<RemoteProcedureCallFlow>(_count);

        if (_count == 0)
        {
            return flows;
        }

        var index = GetNewestIndex();

        for (var itemIndex = 0; itemIndex < _count; itemIndex++)
        {
            var flowIdentifier = _order[index];

            if (_flows.TryGetValue(flowIdentifier, out RemoteProcedureCallFlow? flow))
            {
                flows.Add(flow);
            }

            index = GetPreviousIndex(index);
        }

        return flows;
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

    private int GetPreviousIndex(int index)
    {
        if (index == 0)
        {
            return Capacity - 1;
        }

        return index - 1;
    }

    private void RemoveOldestFlowWhenFull()
    {
        if (_count == Capacity)
        {
            var flowIdentifier = _order[_nextIndex];
            _flows.TryRemove(flowIdentifier, out _);
            return;
        }

        _count++;
    }
}
