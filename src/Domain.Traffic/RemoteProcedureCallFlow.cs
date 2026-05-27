using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents a captured Remote Procedure Call (gRPC) stream. Wraps the parent
///     <see cref="TrafficFlow" /> produced by the HTTP/2 request and exposes an append-only
///     chronological collection of <see cref="RemoteProcedureCallCapturedMessage" /> instances
///     covering both request and response messages.
/// </summary>
public sealed class RemoteProcedureCallFlow
{
    private readonly Lock _gate;
    private readonly List<RemoteProcedureCallCapturedMessage> _messages;
    private DateTimeOffset? _closedAt;

    /// <summary>
    ///     Gets the wall-clock instant the stream was observed to close, or null while open.
    /// </summary>
    public DateTimeOffset? ClosedAt
    {
        get
        {
            lock (_gate)
            {
                return _closedAt;
            }
        }
    }

    /// <summary>
    ///     Gets the underlying HTTP/2 traffic flow.
    /// </summary>
    public TrafficFlow Flow { get; }

    /// <summary>
    ///     Gets the unique identifier inherited from <see cref="Flow" />.
    /// </summary>
    public Guid Id => Flow.Id;

    /// <summary>
    ///     Gets a value indicating whether the stream has been observed to close.
    /// </summary>
    public bool IsClosed
    {
        get
        {
            lock (_gate)
            {
                return _closedAt.HasValue;
            }
        }
    }

    /// <summary>
    ///     Gets the chronological list of captured messages (request and response interleaved).
    /// </summary>
    public IReadOnlyList<RemoteProcedureCallCapturedMessage> Messages { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallFlow" />.
    /// </summary>
    /// <param name="flow">The HTTP/2 traffic flow underlying this gRPC call.</param>
    public RemoteProcedureCallFlow(TrafficFlow flow)
    {
        var gate = new Lock();
        _gate = gate;
        var messages = new List<RemoteProcedureCallCapturedMessage>();
        _messages = messages;
        var readOnly = new ReadOnlyCollection<RemoteProcedureCallCapturedMessage>(messages);
        Messages = readOnly;
        Flow = flow;
        _closedAt = null;
    }

    /// <summary>
    ///     Records that the stream has been closed. Only the first observation is retained.
    /// </summary>
    /// <param name="closedAt">The wall-clock instant the close was observed.</param>
    public void MarkClosed(DateTimeOffset closedAt)
    {
        lock (_gate)
        {
            if (_closedAt.HasValue)
            {
                return;
            }

            _closedAt = closedAt;
        }
    }

    /// <summary>
    ///     Appends the supplied message to the chronological message list.
    /// </summary>
    /// <param name="message">The message to record.</param>
    public void RecordMessage(RemoteProcedureCallCapturedMessage message)
    {
        lock (_gate)
        {
            _messages.Add(message);
        }
    }
}
