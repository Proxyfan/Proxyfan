using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Defines storage operations for captured Server-Sent Events (SSE) streams. An SSE flow is
///     created when an HTTP response is detected to have <c>Content-Type: text/event-stream</c>
///     and the proxy begins relaying events from the upstream server to the downstream client.
/// </summary>
public interface IServerSentEventsStore
{
    /// <summary>
    ///     Gets the configured SSE flow capacity.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    ///     Gets the current number of stored SSE flows.
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Adds an SSE flow to the store.
    /// </summary>
    /// <param name="flow">The flow to store.</param>
    void Add(ServerSentEventsFlow flow);

    /// <summary>
    ///     Removes all stored SSE flows.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Returns all stored SSE flows ordered from newest to oldest.
    /// </summary>
    /// <returns>A snapshot of the currently stored SSE flows.</returns>
    IReadOnlyList<ServerSentEventsFlow> GetAll();

    /// <summary>
    ///     Looks up a stored SSE flow by identifier.
    /// </summary>
    /// <param name="id">The flow identifier.</param>
    /// <returns>The stored SSE flow when found; otherwise, <see langword="null" />.</returns>
    ServerSentEventsFlow? GetById(Guid id);
}
