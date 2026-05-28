using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Defines storage operations for captured WebSocket conversations. A WebSocket flow is
///     created when an HTTP/1.1 Upgrade exchange succeeds (response 101) and the connection
///     transitions into RFC 6455 frame-based messaging.
/// </summary>
public interface IWebSocketStore
{
    /// <summary>
    ///     Gets the configured WebSocket flow capacity.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    ///     Gets the current number of stored WebSocket flows.
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Adds a WebSocket flow to the store.
    /// </summary>
    /// <param name="flow">The flow to store.</param>
    void Add(WebSocketFlow flow);

    /// <summary>
    ///     Removes all stored WebSocket flows.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Returns all stored WebSocket flows ordered from newest to oldest.
    /// </summary>
    /// <returns>A snapshot of the currently stored WebSocket flows.</returns>
    IReadOnlyList<WebSocketFlow> GetAll();

    /// <summary>
    ///     Looks up a stored WebSocket flow by identifier.
    /// </summary>
    /// <param name="id">The flow identifier.</param>
    /// <returns>The stored WebSocket flow when found; otherwise, <see langword="null" />.</returns>
    WebSocketFlow? GetById(Guid id);
}
