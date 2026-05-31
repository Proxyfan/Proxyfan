using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Serialization;
using System;
using System.Buffers;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Per-HTTP/2-stream gRPC message-extraction state owned by the
///     <see cref="HypertextTransferProtocolVersion2Orchestrator" />. Buffers partial
///     length-prefixed messages across multiple DATA frames, fans completed messages into
///     the supplied <see cref="RemoteProcedureCallFlow" />, and tracks the wall-clock
///     timestamp for each captured message via the bundled
///     <see cref="System.TimeProvider" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2RemoteProcedureCallCapture
{
    private readonly ArrayBufferWriter<byte> _clientBuffer;
    private readonly TimeProvider _timeProvider;
    private readonly ArrayBufferWriter<byte> _upstreamBuffer;

    /// <summary>
    ///     Gets the underlying gRPC flow that captures messages extracted by this state object.
    /// </summary>
    public RemoteProcedureCallFlow Flow { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolVersion2RemoteProcedureCallCapture" />.
    /// </summary>
    /// <param name="flow">The gRPC flow to append captured messages to.</param>
    /// <param name="timeProvider">The wall-clock time source for message timestamps.</param>
    public HypertextTransferProtocolVersion2RemoteProcedureCallCapture(
        RemoteProcedureCallFlow flow,
        TimeProvider timeProvider)
    {
        Flow = flow;
        _timeProvider = timeProvider;
        var clientBuffer = new ArrayBufferWriter<byte>();
        _clientBuffer = clientBuffer;
        var upstreamBuffer = new ArrayBufferWriter<byte>();
        _upstreamBuffer = upstreamBuffer;
    }

    /// <summary>
    ///     Appends DATA bytes captured from the client-to-upstream direction and emits any
    ///     newly-completed gRPC messages on the request side.
    /// </summary>
    /// <param name="data">The DATA payload (after padding removal).</param>
    public void AppendClientBytes(ReadOnlySpan<byte> data)
    {
        _clientBuffer.Write(data);
        ExtractMessages(_clientBuffer, RemoteProcedureCallDirection.Outbound);
    }

    /// <summary>
    ///     Appends DATA bytes captured from the upstream-to-client direction and emits any
    ///     newly-completed gRPC messages on the response side.
    /// </summary>
    /// <param name="data">The DATA payload (after padding removal).</param>
    public void AppendUpstreamBytes(ReadOnlySpan<byte> data)
    {
        _upstreamBuffer.Write(data);
        ExtractMessages(_upstreamBuffer, RemoteProcedureCallDirection.Inbound);
    }

    private void ExtractMessages(ArrayBufferWriter<byte> buffer, RemoteProcedureCallDirection direction)
    {
        RemoteProcedureCallExtractionResult result;
        try
        {
            result = RemoteProcedureCallMessageExtractor.ExtractAvailable(buffer.WrittenMemory);
        }
        catch (System.IO.InvalidDataException)
        {
            return;
        }

        if (result.Messages.Count == 0)
        {
            return;
        }

        var timestamp = _timeProvider.GetUtcNow();
        for (var index = 0; index < result.Messages.Count; index++)
        {
            var message = result.Messages[index];
            var captured = new RemoteProcedureCallCapturedMessage(direction, message.IsCompressed, message.Payload, timestamp);
            Flow.RecordMessage(captured);
        }

        var consumed = result.BytesConsumed;
        var remaining = buffer.WrittenSpan[consumed..].ToArray();
        buffer.ResetWrittenCount();
        if (remaining.Length > 0)
        {
            buffer.Write(remaining);
        }
    }
}
