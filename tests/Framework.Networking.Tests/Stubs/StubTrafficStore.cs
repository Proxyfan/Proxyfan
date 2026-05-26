using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="ITrafficStore" /> that records all added flows
///     for assertion in unit tests.
/// </summary>
public sealed class StubTrafficStore : ITrafficStore
{
    private readonly List<TrafficFlow> _flows;

    /// <summary>
    ///     Gets all traffic flows added to this store in order of addition.
    /// </summary>
    public IReadOnlyList<TrafficFlow> AddedFlows => _flows;

    /// <summary>
    ///     Initializes a new instance of <see cref="StubTrafficStore" />.
    /// </summary>
    public StubTrafficStore()
    {
        List<TrafficFlow> flows = [];
        _flows = flows;
    }

    /// <inheritdoc />
    public void Add(TrafficFlow flow)
    {
        _flows.Add(flow);
    }

    /// <inheritdoc />
    public int Capacity => int.MaxValue;

    /// <inheritdoc />
    public void Clear()
    {
        _flows.Clear();
    }

    /// <inheritdoc />
    public int Count => _flows.Count;

    /// <inheritdoc />
    public IReadOnlyList<TrafficFlow> GetAll()
    {
        return _flows;
    }

    /// <inheritdoc />
    public TrafficFlow? GetById(Guid id)
    {
        foreach (var flow in _flows)
        {
            if (flow.Id == id)
            {
                return flow;
            }
        }

        return null;
    }
}
