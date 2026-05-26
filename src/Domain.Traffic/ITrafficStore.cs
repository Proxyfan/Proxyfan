using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Defines storage operations for captured traffic flows.
/// </summary>
public interface ITrafficStore
{
    /// <summary>
    ///     Gets the configured flow capacity.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    ///     Gets the current number of stored flows.
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Adds a traffic flow to the store.
    /// </summary>
    /// <param name="flow">
    ///     The flow to store.
    /// </param>
    void Add(TrafficFlow flow);

    /// <summary>
    ///     Removes all stored flows.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Returns all stored flows ordered from newest to oldest.
    /// </summary>
    /// <returns>
    ///     A snapshot of the currently stored flows.
    /// </returns>
    IReadOnlyList<TrafficFlow> GetAll();

    /// <summary>
    ///     Looks up a stored flow by identifier.
    /// </summary>
    /// <param name="id">
    ///     The flow identifier.
    /// </param>
    /// <returns>
    ///     The stored flow when found; otherwise, <see langword="null" />.
    /// </returns>
    TrafficFlow? GetById(Guid id);
}