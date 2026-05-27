using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Proxyfan.Domain.Traffic.Diff;

/// <summary>
///     An in-memory pool of <see cref="TrafficFlow" /> candidates that the user has
///     marked for diff comparison. The pool is bounded (oldest entries are evicted)
///     and raises <see cref="Changed" /> whenever its contents change so UI elements
///     can refresh selection lists.
/// </summary>
public sealed class TrafficFlowDiffPool
{
    /// <summary>
    ///     Raised when a flow is added to or removed from the pool.
    /// </summary>
    public event TrafficFlowDiffPoolChanged? Changed;

    /// <summary>
    ///     The default maximum number of flows retained in the pool. Older entries
    ///     are evicted when this limit is exceeded.
    /// </summary>
    public const int DefaultCapacity = 16;
    private readonly LinkedList<TrafficFlow> _flows;

    /// <summary>
    ///     Gets the maximum number of flows this pool retains before eviction.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    ///     Gets the current number of flows in the pool.
    /// </summary>
    public int Count => _flows.Count;

    /// <summary>
    ///     Initializes a new <see cref="TrafficFlowDiffPool" /> with the default
    ///     capacity (<see cref="DefaultCapacity" />).
    /// </summary>
    public TrafficFlowDiffPool()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficFlowDiffPool" /> with an explicit
    ///     capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of flows to retain. Must be positive.</param>
    public TrafficFlowDiffPool(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }

        var flows = new LinkedList<TrafficFlow>();
        _flows = flows;
        Capacity = capacity;
    }

    /// <summary>
    ///     Adds <paramref name="flow" /> to the pool. Duplicate flows (by reference)
    ///     are ignored silently. If the pool is at capacity, the oldest entry is
    ///     evicted to make room.
    /// </summary>
    /// <param name="flow">The flow to add.</param>
    public void Add(TrafficFlow flow)
    {
        if (_flows.Contains(flow))
        {
            return;
        }

        if (_flows.Count >= Capacity)
        {
            _flows.RemoveFirst();
        }

        _flows.AddLast(flow);
        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Removes every flow from the pool. Raises <see cref="Changed" /> only when
    ///     the pool was non-empty.
    /// </summary>
    public void Clear()
    {
        if (_flows.Count == 0)
        {
            return;
        }

        _flows.Clear();
        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Removes <paramref name="flow" /> from the pool, if present. Absent flows
    ///     are ignored silently.
    /// </summary>
    /// <param name="flow">The flow to remove.</param>
    public void Remove(TrafficFlow flow)
    {
        if (!_flows.Remove(flow))
        {
            return;
        }

        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Returns a snapshot of the flows currently in the pool, ordered from oldest
    ///     to most recently added.
    /// </summary>
    /// <returns>
    ///     A new read-only collection of the pool's flows.
    /// </returns>
    public ReadOnlyCollection<TrafficFlow> Snapshot()
    {
        var array = new TrafficFlow[_flows.Count];
        var index = 0;
        foreach (var flow in _flows)
        {
            array[index] = flow;
            index++;
        }

        var snapshot = new ReadOnlyCollection<TrafficFlow>(array);
        return snapshot;
    }
}
