using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Reads length-prefixed gRPC (Google Remote Procedure Call) messages from a source
///     stream, forwards the raw bytes verbatim to a destination stream, and invokes a
///     <see cref="RemoteProcedureCallMessageCallback" /> for every fully-extracted message.
///     One instance handles one direction; a bidirectional streaming RPC runs two concurrent
///     relay instances (one outbound, one inbound).
/// </summary>
public sealed class RemoteProcedureCallRelay
{
    private readonly RemoteProcedureCallDirection _direction;
    private readonly RemoteProcedureCallMessageCallback _onMessage;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallRelay" />.
    /// </summary>
    /// <param name="direction">The direction this relay handles.</param>
    /// <param name="onMessage">Callback invoked for every captured message.</param>
    /// <param name="timeProvider">Time source used for message timestamps.</param>
    public RemoteProcedureCallRelay(
        RemoteProcedureCallDirection direction,
        RemoteProcedureCallMessageCallback onMessage,
        TimeProvider timeProvider)
    {
        _direction = direction;
        _onMessage = onMessage;
        _timeProvider = timeProvider;
    }

    /// <summary>
    ///     Relays bytes from <paramref name="source" /> to <paramref name="destination" />
    ///     while extracting complete gRPC messages. Returns when the source stream closes or
    ///     cancellation is requested.
    /// </summary>
    /// <param name="source">The stream to read from.</param>
    /// <param name="destination">The stream to write to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages captured during this relay.</returns>
    public async Task<int> RelayAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var accumulator = new MemoryStream();
        var capturedMessages = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);

            await accumulator.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            capturedMessages += DrainCompletedMessages(accumulator);
        }

        return capturedMessages;
    }

    private int DrainCompletedMessages(MemoryStream accumulator)
    {
        var raw = accumulator.ToArray();
        var extraction = RemoteProcedureCallMessageExtractor.ExtractAvailable(raw);

        for (var index = 0; index < extraction.Messages.Count; index++)
        {
            var underlying = extraction.Messages[index];
            var timestamp = _timeProvider.GetUtcNow();
            var captured = new RemoteProcedureCallCapturedMessage(
                _direction,
                underlying.IsCompressed,
                underlying.Payload,
                timestamp);
            _onMessage(captured);
        }

        if (extraction.BytesConsumed == 0)
        {
            return extraction.Messages.Count;
        }

        var remaining = raw.Length - extraction.BytesConsumed;
        accumulator.SetLength(0);
        if (remaining > 0)
        {
            accumulator.Write(raw, extraction.BytesConsumed, remaining);
        }

        return extraction.Messages.Count;
    }
}
