using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Reads HTTP/2 frames from a <see cref="PipeReader" /> one at a time. The reader honours
///     the contract of <see cref="PipeReader" /> by advancing the reader past the frame on
///     success and leaving it untouched when more bytes are needed. Returns <c>null</c> when
///     the peer cleanly closed the transport before another frame could be read.
/// </summary>
public static class HypertextTransferProtocolVersion2FrameReader
{
    /// <summary>
    ///     Reads the next frame from <paramref name="reader" />. The returned payload is a
    ///     freshly-allocated copy that is safe to retain past the next call.
    /// </summary>
    /// <param name="reader">The pipe to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next frame, or <c>null</c> when the pipe was cleanly completed.</returns>
    public static async Task<HypertextTransferProtocolVersion2Frame?> ReadFrameAsync(
        PipeReader reader,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;
            var consumeResult = HypertextTransferProtocolVersion2FrameReader.TryConsumeFrame(buffer);
            if (consumeResult.Frame is not null)
            {
                reader.AdvanceTo(consumeResult.Consumed, consumeResult.Consumed);
                return consumeResult.Frame;
            }
            if (result.IsCompleted && buffer.IsEmpty)
            {
                reader.AdvanceTo(buffer.End);
                return null;
            }
            reader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    private static FrameConsumeResult TryConsumeFrame(ReadOnlySequence<byte> buffer)
    {
        if (buffer.Length < HypertextTransferProtocolVersion2FrameParser.HeaderLength)
        {
            return new FrameConsumeResult(null, buffer.Start);
        }
        var headerSequence = buffer.Slice(0, HypertextTransferProtocolVersion2FrameParser.HeaderLength);
        var headerBytes = headerSequence.IsSingleSegment ? headerSequence.FirstSpan : headerSequence.ToArray().AsSpan();
        var header = HypertextTransferProtocolVersion2FrameParser.TryParseHeader(headerBytes);
        if (header is null)
        {
            return new FrameConsumeResult(null, buffer.Start);
        }
        var totalLength = HypertextTransferProtocolVersion2FrameParser.HeaderLength + header.Length;
        if (buffer.Length < totalLength)
        {
            return new FrameConsumeResult(null, buffer.Start);
        }
        var payloadSequence = buffer.Slice(HypertextTransferProtocolVersion2FrameParser.HeaderLength, header.Length);
        var payloadCopy = payloadSequence.ToArray();
        var consumed = buffer.GetPosition(totalLength);
        var frame = new HypertextTransferProtocolVersion2Frame(header, payloadCopy);
        return new FrameConsumeResult(frame, consumed);
    }

    private readonly struct FrameConsumeResult
    {
        public SequencePosition Consumed { get; }

        public HypertextTransferProtocolVersion2Frame? Frame { get; }

        public FrameConsumeResult(HypertextTransferProtocolVersion2Frame? frame, SequencePosition consumed)
        {
            Frame = frame;
            Consumed = consumed;
        }
    }
}
