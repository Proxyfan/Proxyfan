using System;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents a captured WebSocket conversation. Wraps the parent <see cref="TrafficFlow" />
///     produced by the original HTTP upgrade request and exposes an append-only collection of
///     <see cref="WebSocketMessage" /> instances captured for the lifetime of the connection.
/// </summary>
public sealed class WebSocketFlow
{
    /// <summary>
    ///     Raised after the flow transitions to a closed state via
    ///     <see cref="MarkClosed" />. Fires at most once per flow.
    ///     Handlers run synchronously on the calling thread; UI subscribers
    ///     must marshal back to the UI thread themselves.
    /// </summary>
    public event WebSocketFlowClosedHandler? Closed;

    /// <summary>
    ///     Raised after a message is appended via <see cref="RecordMessage" />.
    ///     Handlers run synchronously on the calling thread (typically a
    ///     background relay thread); UI subscribers must marshal back to the
    ///     UI thread themselves.
    /// </summary>
    public event WebSocketMessageRecordedHandler? MessageRecorded;

    private const int DefaultMessageCapacity = StreamingCaptureBudgets.WebSocketAndRemoteProcedureCallMessageCapacity;
    private readonly Lock _gate;
    private readonly int _messageCapacity;
    private readonly List<WebSocketMessage> _messages;
    private readonly StreamingCaptureBudget _streamingCaptureBudget;
    private DateTimeOffset? _closedAt;
    private int _droppedMessagesCount;

    /// <summary>
    ///     Gets the wall-clock instant the connection was closed, or null when still open.
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
    ///     Gets the underlying HTTP request/response flow that initiated the WebSocket upgrade.
    /// </summary>
    public TrafficFlow Flow { get; }

    /// <summary>
    ///     Gets the unique identifier inherited from <see cref="Flow" />.
    /// </summary>
    public Guid Id => Flow.Id;

    /// <summary>
    ///     Gets a value indicating whether this WebSocket has been observed to close
    ///     (received a close frame or had its underlying transport torn down).
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
    ///     Gets the chronological list of captured WebSocket messages.
    /// </summary>
    public IReadOnlyList<WebSocketMessage> Messages => GetMessageSnapshot();

    /// <summary>
    ///     Initializes a new <see cref="WebSocketFlow" /> wrapping the supplied HTTP flow.
    /// </summary>
    /// <param name="flow">
    ///     The HTTP traffic flow whose response upgraded to WebSocket.
    /// </param>
    public WebSocketFlow(TrafficFlow flow)
        : this(flow, DefaultMessageCapacity, StreamingCaptureBudgets.Shared)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="WebSocketFlow" /> wrapping the supplied HTTP flow.
    /// </summary>
    /// <param name="flow">
    ///     The HTTP traffic flow whose response upgraded to WebSocket.
    /// </param>
    /// <param name="messageCapacity">The maximum number of retained messages for this flow.</param>
    /// <param name="streamingCaptureBudget">The shared streaming capture budget.</param>
    public WebSocketFlow(TrafficFlow flow, int messageCapacity, StreamingCaptureBudget streamingCaptureBudget)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(messageCapacity);

        var gate = new Lock();
        _gate = gate;
        var messages = new List<WebSocketMessage>();
        _messages = messages;
        Flow = flow;
        _closedAt = null;
        _droppedMessagesCount = 0;
        _messageCapacity = messageCapacity;
        _streamingCaptureBudget = streamingCaptureBudget;
    }

    /// <summary>
    ///     Returns a point-in-time snapshot of captured messages in chronological order.
    /// </summary>
    /// <returns>
    ///     A stable copy of captured messages at the instant the snapshot is taken.
    /// </returns>
    public IReadOnlyList<WebSocketMessage> GetMessageSnapshot()
    {
        lock (_gate)
        {
            return [.. _messages];
        }
    }

    /// <summary>
    ///     Records that the connection has been closed at the supplied instant. Subsequent
    ///     calls are no-ops (the first observed close timestamp wins).
    /// </summary>
    /// <param name="closedAt">The wall-clock instant the close was observed.</param>
    public void MarkClosed(DateTimeOffset closedAt)
    {
        bool fire;
        lock (_gate)
        {
            if (_closedAt.HasValue)
            {
                return;
            }

            _closedAt = closedAt;
            fire = true;
        }

        if (fire)
        {
            RaiseClosed();
        }
    }

    /// <summary>
    ///     Appends the supplied message to the chronological message list.
    /// </summary>
    /// <param name="message">The message to record.</param>
    public void RecordMessage(WebSocketMessage message)
    {
        bool messageRecorded;
        lock (_gate)
        {
            AppendMessageCore(message, out messageRecorded);
        }

        if (messageRecorded)
        {
            RaiseMessageRecorded(message);
        }
    }

    private void AppendMessageCore(WebSocketMessage message, out bool messageRecorded)
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

    private int GetMessageSizeBytes(WebSocketMessage message)
    {
        return message.Payload.Length;
    }

    private void RaiseClosed()
    {
        var handler = Closed;
        if (handler is null)
        {
            return;
        }

        foreach (var subscriber in handler.GetInvocationList())
        {
            if (subscriber is not WebSocketFlowClosedHandler typedSubscriber)
            {
                continue;
            }

            try
            {
                typedSubscriber();
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }

    private void RaiseMessageRecorded(WebSocketMessage message)
    {
        var handler = MessageRecorded;
        if (handler is null)
        {
            return;
        }

        foreach (var subscriber in handler.GetInvocationList())
        {
            if (subscriber is not WebSocketMessageRecordedHandler typedSubscriber)
            {
                continue;
            }

            try
            {
                typedSubscriber(message);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }
}
