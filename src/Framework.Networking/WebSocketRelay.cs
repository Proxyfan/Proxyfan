using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Reads RFC 6455 WebSocket frames from a source stream, forwards the raw bytes verbatim
///     to a destination stream, and invokes a <see cref="WebSocketMessageCallback" /> for every
///     fully-reassembled <see cref="WebSocketMessage" />. The relay never modifies the wire
///     bytes - masks and payloads pass through untouched so the destination peer sees exactly
///     what the source sent. One instance handles one direction; a full bidirectional relay
///     runs two instances concurrently.
/// </summary>
public sealed class WebSocketRelay
{
    private readonly WebSocketMessageAssembler _assembler;
    private readonly WebSocketDirection _direction;
    private readonly WebSocketMessageCallback _onMessage;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new <see cref="WebSocketRelay" />.
    /// </summary>
    /// <param name="direction">The direction this relay handles.</param>
    /// <param name="onMessage">Callback invoked for every fully-reassembled message.</param>
    /// <param name="timeProvider">Time source used for message timestamps.</param>
    public WebSocketRelay(WebSocketDirection direction, WebSocketMessageCallback onMessage, TimeProvider timeProvider)
    {
        var assembler = new WebSocketMessageAssembler();
        _assembler = assembler;
        _direction = direction;
        _onMessage = onMessage;
        _timeProvider = timeProvider;
    }

    /// <summary>
    ///     Relays frames from <paramref name="source" /> to <paramref name="destination" />
    ///     until the source stream is closed, a Close frame is observed, or cancellation is
    ///     requested.
    /// </summary>
    /// <param name="source">The stream to read from.</param>
    /// <param name="destination">The stream to write to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of fully-reassembled messages captured during this relay.</returns>
    public async Task<int> RelayAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var accumulator = new MemoryStream();
        var capturedMessages = 0;
        var observedCloseFrame = false;

        while (!observedCloseFrame)
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
            var drain = DrainCompletedFrames(accumulator);
            capturedMessages += drain.MessagesCaptured;
            if (drain.IsObservedCloseFrame)
            {
                observedCloseFrame = true;
            }
        }

        return capturedMessages;
    }

    private DrainResult DrainCompletedFrames(MemoryStream accumulator)
    {
        var captured = 0;
        var sawClose = false;
        var buffer = accumulator.ToArray();
        var offset = 0;

        while (offset < buffer.Length)
        {
            var slice = new ReadOnlyMemory<byte>(buffer, offset, buffer.Length - offset);
            var frame = WebSocketFrameParser.TryParse(slice);
            if (frame is null)
            {
                break;
            }

            offset += frame.TotalLength;

            var timestamp = _timeProvider.GetUtcNow();
            var message = _assembler.Accept(frame, _direction, timestamp);
            if (message is not null)
            {
                _onMessage(message);
                captured++;

                if (message.Opcode == WebSocketOpcode.Close)
                {
                    sawClose = true;
                    break;
                }
            }
        }

        if (offset == 0)
        {
            var noopResult = new DrainResult(captured, sawClose);
            return noopResult;
        }

        var remaining = buffer.Length - offset;
        accumulator.SetLength(0);
        if (remaining > 0)
        {
            accumulator.Write(buffer, offset, remaining);
        }

        var result = new DrainResult(captured, sawClose);
        return result;
    }

    private readonly record struct DrainResult
    {
        public bool IsObservedCloseFrame { get; }

        public int MessagesCaptured { get; }

        public DrainResult(int messagesCaptured, bool observedCloseFrame)
        {
            MessagesCaptured = messagesCaptured;
            IsObservedCloseFrame = observedCloseFrame;
        }
    }
}
