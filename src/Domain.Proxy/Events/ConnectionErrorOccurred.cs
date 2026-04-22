using System;
using System.Net;

namespace Proxyfan.Domain.Proxy.Events;

/// <summary>
///     Published when a connection handler throws an unhandled exception while processing
///     an accepted connection. The connection is closed after this event is published.
/// </summary>
/// <param name="RemoteEndPoint">The remote endpoint of the client whose connection failed.</param>
/// <param name="Exception">The exception that caused the connection to fail.</param>
/// <param name="Timestamp">The UTC instant at which the error occurred.</param>
public sealed record ConnectionErrorOccurred(
    EndPoint RemoteEndPoint,
    Exception Exception,
    DateTimeOffset Timestamp) : IDomainEvent;
