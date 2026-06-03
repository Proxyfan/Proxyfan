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
        var accumulator = new FrameAccumulator(8192);
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

            accumulator.Append(buffer, 0, bytesRead);

            var drain = DrainCompletedFrames(accumulator);
            capturedMessages += drain.MessagesCaptured;
            if (drain.IsObservedCloseFrame)
            {
                observedCloseFrame = true;
            }
        }

        return capturedMessages;
    }

    private DrainResult DrainCompletedFrames(FrameAccumulator accumulator)
    {
        var captured = 0;
        var sawClose = false;
        var offset = 0;

        while (offset < accumulator.Length)
        {
            var slice = new ReadOnlyMemory<byte>(accumulator.Buffer, offset, accumulator.Length - offset);
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

        accumulator.Consume(offset);

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

    /// <summary>
    ///     Mutable byte accumulator that grows on demand and compacts only the unconsumed
    ///     tail when frames are drained. Keeps the parser working on a stable
    ///     <see cref="ReadOnlyMemory{Byte}" /> slice without copying already-buffered bytes
    ///     on every read.
    /// </summary>
    private sealed class FrameAccumulator
    {
        public byte[] Buffer { get; private set; }

        public int Length { get; private set; }

        public FrameAccumulator(int initialCapacity)
        {
            var initial = new byte[initialCapacity];
            Buffer = initial;
            Length = 0;
        }

        public void Append(byte[] source, int offset, int count)
        {
            EnsureCapacity(Length + count);
            System.Buffer.BlockCopy(source, offset, Buffer, Length, count);
            Length += count;
        }

        public void Consume(int consumed)
        {
            var remaining = Length - consumed;
            if (remaining > 0)
            {
                System.Buffer.BlockCopy(Buffer, consumed, Buffer, 0, remaining);
            }

            Length = remaining;
        }

        private void EnsureCapacity(int requiredLength)
        {
            if (requiredLength <= Buffer.Length)
            {
                return;
            }

            var newSize = Buffer.Length;
            while (newSize < requiredLength)
            {
                newSize *= 2;
            }

            var resized = new byte[newSize];
            System.Buffer.BlockCopy(Buffer, 0, resized, 0, Length);
            Buffer = resized;
        }
    }
}
