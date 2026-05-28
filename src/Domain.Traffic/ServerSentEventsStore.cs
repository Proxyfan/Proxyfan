using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Stores Server-Sent Events (SSE) flows in a bounded in-memory ring buffer. Mirrors
///     <see cref="WebSocketStore" /> in semantics but holds <see cref="ServerSentEventsFlow" />
///     instances which append events over time after the initial HTTP response is detected to be
///     <c>text/event-stream</c>.
/// </summary>
public sealed class ServerSentEventsStore : IServerSentEventsStore
{
    private const int DefaultCapacity = 10000;
    private readonly ConcurrentDictionary<Guid, ServerSentEventsFlow> _flows;
    private readonly Guid[] _order;
    private readonly object _syncRoot;
    private int _count;
    private int _nextIndex;

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEventsStore" /> with the default capacity.
    /// </summary>
    public ServerSentEventsStore()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEventsStore" /> with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of SSE flows to retain.</param>
    public ServerSentEventsStore(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        Capacity = capacity;
        _count = 0;
        _nextIndex = 0;

        var flows = new ConcurrentDictionary<Guid, ServerSentEventsFlow>();
        var order = new Guid[capacity];
        var syncRoot = new object();
        _flows = flows;
        _order = order;
        _syncRoot = syncRoot;
    }

    /// <summary>
    ///     Adds an SSE flow to the store.
    /// </summary>
    /// <param name="flow">The flow to store.</param>
    public void Add(ServerSentEventsFlow flow)
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
    ///     Gets the configured SSE flow capacity.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    ///     Removes all stored SSE flows.
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
    ///     Gets the current number of stored SSE flows.
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
    ///     Returns all stored SSE flows ordered from newest to oldest.
    /// </summary>
    /// <returns>A snapshot of the currently stored SSE flows.</returns>
    public IReadOnlyList<ServerSentEventsFlow> GetAll()
    {
        lock (_syncRoot)
        {
            return CreateSnapshot();
        }
    }

    /// <summary>
    ///     Looks up a stored SSE flow by identifier.
    /// </summary>
    /// <param name="id">The flow identifier.</param>
    /// <returns>The stored SSE flow when found; otherwise, <see langword="null" />.</returns>
    public ServerSentEventsFlow? GetById(Guid id)
    {
        if (_flows.TryGetValue(id, out ServerSentEventsFlow? flow))
        {
            return flow;
        }

        return null;
    }

    private List<ServerSentEventsFlow> CreateSnapshot()
    {
        var flows = new List<ServerSentEventsFlow>(_count);

        if (_count == 0)
        {
            return flows;
        }

        var index = GetNewestIndex();

        for (var itemIndex = 0; itemIndex < _count; itemIndex++)
        {
            var flowIdentifier = _order[index];

            if (_flows.TryGetValue(flowIdentifier, out ServerSentEventsFlow? flow))
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
