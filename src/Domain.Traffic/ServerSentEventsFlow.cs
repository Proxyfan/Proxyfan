using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents a captured Server-Sent Events (SSE) stream. Wraps the parent
///     <see cref="TrafficFlow" /> produced by the original HTTP GET request and exposes an
///     append-only chronological collection of <see cref="ServerSentEvent" /> instances.
/// </summary>
public sealed class ServerSentEventsFlow
{
    /// <summary>
    ///     Raised when <see cref="MarkClosed" /> records the first close observation.
    /// </summary>
    public event ServerSentEventsFlowClosedHandler? Closed;

    /// <summary>
    ///     Raised whenever <see cref="RecordEvent" /> appends a new event.
    /// </summary>
    public event ServerSentEventsFlowEventRecordedHandler? EventRecorded;

    private const int DefaultEventCapacity = StreamingCaptureBudgets.ServerSentEventsEventCapacity;
    private readonly int _eventCapacity;
    private readonly List<ServerSentEvent> _events;
    private readonly Lock _gate;
    private readonly StreamingCaptureBudget _streamingCaptureBudget;
    private DateTimeOffset? _closedAt;
    private int _droppedMessagesCount;

    /// <summary>
    ///     Gets the wall-clock instant the stream was observed to close (or null while still open).
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
    ///     Gets the total number of captured events dropped by capacity or global budget limits.
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
    ///     Gets the chronological list of captured events.
    /// </summary>
    public IReadOnlyList<ServerSentEvent> Events
    {
        get
        {
            lock (_gate)
            {
                return [.. _events];
            }
        }
    }

    /// <summary>
    ///     Gets the underlying HTTP request/response flow that initiated the SSE stream.
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
    ///     Initializes a new <see cref="ServerSentEventsFlow" />.
    /// </summary>
    /// <param name="flow">The HTTP traffic flow whose response is an SSE stream.</param>
    public ServerSentEventsFlow(TrafficFlow flow)
        : this(flow, DefaultEventCapacity, StreamingCaptureBudgets.Shared)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEventsFlow" />.
    /// </summary>
    /// <param name="flow">The HTTP traffic flow whose response is an SSE stream.</param>
    /// <param name="eventCapacity">The maximum number of retained events for this flow.</param>
    /// <param name="streamingCaptureBudget">The shared streaming capture budget.</param>
    public ServerSentEventsFlow(TrafficFlow flow, int eventCapacity, StreamingCaptureBudget streamingCaptureBudget)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(eventCapacity);

        var gate = new Lock();
        _gate = gate;
        List<ServerSentEvent> events = [];
        _events = events;
        Flow = flow;
        _closedAt = null;
        _droppedMessagesCount = 0;
        _eventCapacity = eventCapacity;
        _streamingCaptureBudget = streamingCaptureBudget;
    }

    /// <summary>
    ///     Returns a stable, immutable snapshot of the current event list and closed state,
    ///     captured atomically under the producer lock. Use this when attaching an observer
    ///     to seed initial state without racing against concurrent <see cref="RecordEvent" /> calls.
    /// </summary>
    /// <returns>
    ///     A <see cref="ServerSentEventsFlowSnapshot" /> containing a read-only copy of all
    ///     events recorded so far and a flag indicating whether the stream has already closed.
    /// </returns>
    public ServerSentEventsFlowSnapshot GetEventsSnapshot()
    {
        lock (_gate)
        {
            var snapshot = new List<ServerSentEvent>(_events);
            return new ServerSentEventsFlowSnapshot
            {
                Events = snapshot,
                IsClosed = _closedAt.HasValue,
            };
        }
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
    ///     Appends the supplied event to the chronological event list.
    /// </summary>
    /// <param name="serverSentEvent">The event to record.</param>
    public void RecordEvent(ServerSentEvent serverSentEvent)
    {
        bool eventRecorded;
        lock (_gate)
        {
            AppendEventCore(serverSentEvent, out eventRecorded);
        }

        if (eventRecorded)
        {
            EventRecorded?.Invoke(serverSentEvent);
        }
    }

    private void AppendEventCore(ServerSentEvent serverSentEvent, out bool eventRecorded)
    {
        var incomingEventSizeBytes = GetEventSizeBytes(serverSentEvent);
        if (_events.Count < _eventCapacity)
        {
            if (!_streamingCaptureBudget.CanReserve(incomingEventSizeBytes))
            {
                _droppedMessagesCount++;
                eventRecorded = false;
                return;
            }

            _events.Add(serverSentEvent);
            eventRecorded = true;
            return;
        }

        var oldestEvent = _events[0];
        var oldestEventSizeBytes = GetEventSizeBytes(oldestEvent);
        if (!_streamingCaptureBudget.CanReplaceReservation(oldestEventSizeBytes, incomingEventSizeBytes))
        {
            _droppedMessagesCount++;
            eventRecorded = false;
            return;
        }

        _events.RemoveAt(0);
        _events.Add(serverSentEvent);
        _droppedMessagesCount++;
        eventRecorded = true;
    }

    private int GetEventSizeBytes(ServerSentEvent serverSentEvent)
    {
        var byteCount = Encoding.UTF8.GetByteCount(serverSentEvent.Data);

        if (serverSentEvent.EventType is not null)
        {
            byteCount += Encoding.UTF8.GetByteCount(serverSentEvent.EventType);
        }

        if (serverSentEvent.Id is not null)
        {
            byteCount += Encoding.UTF8.GetByteCount(serverSentEvent.Id);
        }

        if (serverSentEvent.RetryMilliseconds.HasValue)
        {
            byteCount += sizeof(int);
        }

        return byteCount;
    }
}
