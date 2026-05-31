using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Defines storage operations for captured Remote Procedure Call (gRPC) streams. An RPC
///     flow is created when an HTTP/2 response is detected to have
///     <c>Content-Type: application/grpc</c> (or one of its subtypes) and the orchestrator
///     begins extracting length-prefixed messages from DATA frames in either direction.
/// </summary>
public interface IRemoteProcedureCallStore
{
    /// <summary>
    ///     Gets the configured gRPC flow capacity.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    ///     Gets the current number of stored gRPC flows.
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Adds a gRPC flow to the store.
    /// </summary>
    /// <param name="flow">The flow to store.</param>
    void Add(RemoteProcedureCallFlow flow);

    /// <summary>
    ///     Removes all stored gRPC flows.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Returns all stored gRPC flows ordered from newest to oldest.
    /// </summary>
    /// <returns>A snapshot of the currently stored gRPC flows.</returns>
    IReadOnlyList<RemoteProcedureCallFlow> GetAll();

    /// <summary>
    ///     Looks up a stored gRPC flow by identifier.
    /// </summary>
    /// <param name="id">The flow identifier.</param>
    /// <returns>The stored gRPC flow when found; otherwise, <see langword="null" />.</returns>
    RemoteProcedureCallFlow? GetById(Guid id);
}
