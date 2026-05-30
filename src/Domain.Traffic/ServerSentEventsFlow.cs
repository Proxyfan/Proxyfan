using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    private readonly List<ServerSentEvent> _events;
    private readonly Lock _gate;
    private DateTimeOffset? _closedAt;

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
    ///     Gets the chronological list of captured events.
    /// </summary>
    public IReadOnlyList<ServerSentEvent> Events { get; }

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
    {
        var gate = new Lock();
        _gate = gate;
        var events = new List<ServerSentEvent>();
        _events = events;
        var readOnlyEvents = new ReadOnlyCollection<ServerSentEvent>(events);
        Events = readOnlyEvents;
        Flow = flow;
        _closedAt = null;
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
        lock (_gate)
        {
            _events.Add(serverSentEvent);
        }

        EventRecorded?.Invoke(serverSentEvent);
    }
}
