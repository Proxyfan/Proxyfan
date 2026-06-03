using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Stores WebSocket flows in a bounded in-memory ring buffer. Mirrors <see cref="TrafficStore" />
///     in semantics but holds <see cref="WebSocketFlow" /> instances which append messages over time
///     after the initial HTTP upgrade exchange completes.
/// </summary>
public sealed class WebSocketStore : IWebSocketStore
{
    private const int DefaultCapacity = 10000;
    private readonly ConcurrentDictionary<Guid, WebSocketFlow> _flows;
    private readonly Guid[] _order;
    private readonly object _syncRoot;
    private int _count;
    private int _nextIndex;

    /// <summary>
    ///     Initializes a new <see cref="WebSocketStore" /> with the default capacity.
    /// </summary>
    public WebSocketStore()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="WebSocketStore" /> with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of WebSocket flows to retain.</param>
    public WebSocketStore(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        Capacity = capacity;
        _count = 0;
        _nextIndex = 0;

        var flows = new ConcurrentDictionary<Guid, WebSocketFlow>();
        var order = new Guid[capacity];
        var syncRoot = new object();
        _flows = flows;
        _order = order;
        _syncRoot = syncRoot;
    }

    /// <summary>
    ///     Adds a WebSocket flow to the store.
    /// </summary>
    /// <param name="flow">The flow to store.</param>
    public void Add(WebSocketFlow flow)
    {
        lock (_syncRoot)
        {
            if (_flows.ContainsKey(flow.Id))
            {
                _flows[flow.Id] = flow;
                return;
            }

            RemoveOldestFlowWhenFull();
            _flows[flow.Id] = flow;
            _order[_nextIndex] = flow.Id;
            _nextIndex = GetNextIndex(_nextIndex);
        }
    }

    /// <summary>
    ///     Gets the configured WebSocket flow capacity.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    ///     Removes all stored WebSocket flows.
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
    ///     Gets the current number of stored WebSocket flows.
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
    ///     Returns all stored WebSocket flows ordered from newest to oldest.
    /// </summary>
    /// <returns>A snapshot of the currently stored WebSocket flows.</returns>
    public IReadOnlyList<WebSocketFlow> GetAll()
    {
        lock (_syncRoot)
        {
            return CreateSnapshot();
        }
    }

    /// <summary>
    ///     Looks up a stored WebSocket flow by identifier.
    /// </summary>
    /// <param name="id">The flow identifier.</param>
    /// <returns>The stored WebSocket flow when found; otherwise, <see langword="null" />.</returns>
    public WebSocketFlow? GetById(Guid id)
    {
        if (_flows.TryGetValue(id, out WebSocketFlow? flow))
        {
            return flow;
        }

        return null;
    }

    private List<WebSocketFlow> CreateSnapshot()
    {
        var flows = new List<WebSocketFlow>(_count);

        if (_count == 0)
        {
            return flows;
        }

        var index = GetNewestIndex();

        for (var itemIndex = 0; itemIndex < _count; itemIndex++)
        {
            var flowIdentifier = _order[index];

            if (_flows.TryGetValue(flowIdentifier, out WebSocketFlow? flow))
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
