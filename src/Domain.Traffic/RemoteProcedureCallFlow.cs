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
    /// <summary>
    ///     Raised when <see cref="MarkClosed" /> records the first close observation.
    /// </summary>
    public event RemoteProcedureCallFlowClosedHandler? Closed;

    /// <summary>
    ///     Raised whenever <see cref="RecordMessage" /> appends a new captured message.
    /// </summary>
    public event RemoteProcedureCallFlowMessageRecordedHandler? MessageRecorded;

    private const int DefaultMessageCapacity = StreamingCaptureBudgets.WebSocketAndRemoteProcedureCallMessageCapacity;
    private readonly Lock _gate;
    private readonly int _messageCapacity;
    private readonly List<RemoteProcedureCallCapturedMessage> _messages;
    private readonly StreamingCaptureBudget _streamingCaptureBudget;
    private DateTimeOffset? _closedAt;
    private int _droppedMessagesCount;

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
    ///     Gets the total number of captured messages dropped by capacity or global budget limits.
    /// </summary>
    public int DroppedMessagesCount
    {
        get
        {
            lock (_gate)
            {
                return _droppedMessagesCount;
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
    ///     Gets the gRPC method path (<c>/package.Service/Method</c>) derived from the
    ///     underlying flow's request URI absolute path. Returns <see langword="null" /> when
    ///     the request has not yet been captured or the path cannot be derived.
    /// </summary>
    public string? MethodPath
    {
        get
        {
            var request = Flow.Request;
            if (request is null)
            {
                return null;
            }

            var uri = request.RequestUri;
            if (uri is null)
            {
                return null;
            }

            return uri.AbsolutePath;
        }
    }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallFlow" />.
    /// </summary>
    /// <param name="flow">The HTTP/2 traffic flow underlying this gRPC call.</param>
    public RemoteProcedureCallFlow(TrafficFlow flow)
        : this(flow, DefaultMessageCapacity, StreamingCaptureBudgets.Shared)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallFlow" />.
    /// </summary>
    /// <param name="flow">The HTTP/2 traffic flow underlying this gRPC call.</param>
    /// <param name="messageCapacity">The maximum number of retained messages for this flow.</param>
    /// <param name="streamingCaptureBudget">The shared streaming capture budget.</param>
    public RemoteProcedureCallFlow(TrafficFlow flow, int messageCapacity, StreamingCaptureBudget streamingCaptureBudget)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(messageCapacity);

        var gate = new Lock();
        _gate = gate;
        var messages = new List<RemoteProcedureCallCapturedMessage>();
        _messages = messages;
        var readOnly = new ReadOnlyCollection<RemoteProcedureCallCapturedMessage>(messages);
        Messages = readOnly;
        Flow = flow;
        _closedAt = null;
        _droppedMessagesCount = 0;
        _messageCapacity = messageCapacity;
        _streamingCaptureBudget = streamingCaptureBudget;
    }

    /// <summary>
    ///     Records that the stream has been closed. Only the first observation is retained.
    /// </summary>
    /// <param name="closedAt">The wall-clock instant the close was observed.</param>
    public void MarkClosed(DateTimeOffset closedAt)
    {
        bool isFirstClose;
        lock (_gate)
        {
            if (_closedAt.HasValue)
            {
                isFirstClose = false;
            }
            else
            {
                _closedAt = closedAt;
                isFirstClose = true;
            }
        }

        if (isFirstClose)
        {
            Closed?.Invoke();
        }
    }

    /// <summary>
    ///     Appends the supplied message to the chronological message list.
    /// </summary>
    /// <param name="message">The message to record.</param>
    public void RecordMessage(RemoteProcedureCallCapturedMessage message)
    {
        bool messageRecorded;
        lock (_gate)
        {
            AppendMessageCore(message, out messageRecorded);
        }

        if (messageRecorded)
        {
            MessageRecorded?.Invoke(message);
        }
    }

    private void AppendMessageCore(RemoteProcedureCallCapturedMessage message, out bool messageRecorded)
    {
        var incomingMessageSizeBytes = GetMessageSizeBytes(message);
        if (_messages.Count < _messageCapacity)
        {
            if (!_streamingCaptureBudget.CanReserve(incomingMessageSizeBytes))
            {
                _droppedMessagesCount++;
                messageRecorded = false;
                return;
            }

            _messages.Add(message);
            messageRecorded = true;
            return;
        }

        var oldestMessage = _messages[0];
        var oldestMessageSizeBytes = GetMessageSizeBytes(oldestMessage);
        if (!_streamingCaptureBudget.CanReplaceReservation(oldestMessageSizeBytes, incomingMessageSizeBytes))
        {
            _droppedMessagesCount++;
            messageRecorded = false;
            return;
        }

        _messages.RemoveAt(0);
        _messages.Add(message);
        _droppedMessagesCount++;
        messageRecorded = true;
    }

    private int GetMessageSizeBytes(RemoteProcedureCallCapturedMessage message)
    {
        return message.Payload.Length;
    }
}
