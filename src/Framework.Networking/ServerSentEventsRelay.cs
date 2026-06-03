using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Reads Server-Sent Events (text/event-stream) from a source stream, forwards the raw
///     bytes verbatim to a destination stream, and invokes a <see cref="ServerSentEventCallback" />
///     for every fully-parsed event. The relay never modifies the wire bytes so the downstream
///     client sees exactly what the upstream server sent.
/// </summary>
public sealed class ServerSentEventsRelay
{
    private readonly ServerSentEventCallback _onEvent;
    private readonly ServerSentEventsParser _parser;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEventsRelay" />.
    /// </summary>
    /// <param name="onEvent">Callback invoked for every fully-parsed event.</param>
    /// <param name="timeProvider">Time source used for event timestamps.</param>
    public ServerSentEventsRelay(ServerSentEventCallback onEvent, TimeProvider timeProvider)
    {
        var parser = new ServerSentEventsParser();
        _onEvent = onEvent;
        _parser = parser;
        _timeProvider = timeProvider;
    }

    /// <summary>
    ///     Relays bytes from <paramref name="source" /> to <paramref name="destination" /> while
    ///     parsing them as Server-Sent Events. Returns when the source stream closes or
    ///     cancellation is requested.
    /// </summary>
    /// <param name="source">The stream to read from.</param>
    /// <param name="destination">The stream to write to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of events captured during this relay.</returns>
    public async Task<int> RelayAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var capturedEvents = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                _parser.Complete(_timeProvider.GetUtcNow());
                var finalDrained = _parser.DrainCompletedEvents();
                for (var index = 0; index < finalDrained.Count; index++)
                {
                    _onEvent(finalDrained[index]);
                    capturedEvents++;
                }
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);

            var timestamp = _timeProvider.GetUtcNow();
            _parser.Append(buffer.AsMemory(0, bytesRead), timestamp);

            var drained = _parser.DrainCompletedEvents();
            for (var index = 0; index < drained.Count; index++)
            {
                _onEvent(drained[index]);
                capturedEvents++;
            }
        }

        return capturedEvents;
    }
}
