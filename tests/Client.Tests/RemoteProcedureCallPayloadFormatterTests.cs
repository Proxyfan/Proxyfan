using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallPayloadFormatter" />.
/// </summary>
public sealed class RemoteProcedureCallPayloadFormatterTests
{
    /// <summary>
    ///     The preview of a short payload renders all bytes as space-separated hex.
    /// </summary>
    [Test]
    public async Task FormatPreview_ShortPayload_RendersAllBytesAsHex()
    {
        var message = CreateMessage(new byte[] { 0x01, 0x02, 0x0A });

        var preview = RemoteProcedureCallPayloadFormatter.FormatPreview(message);

        await Assert.That(preview).IsEqualTo("01 02 0A");
    }

    /// <summary>
    ///     An empty payload renders as the literal placeholder text.
    /// </summary>
    [Test]
    public async Task FormatPreview_EmptyPayload_RendersPlaceholder()
    {
        var message = CreateMessage(Array.Empty<byte>());

        var preview = RemoteProcedureCallPayloadFormatter.FormatPreview(message);

        await Assert.That(preview).IsEqualTo("(empty)");
    }

    /// <summary>
    ///     Payloads exceeding the preview length limit are ellipsised.
    /// </summary>
    [Test]
    public async Task FormatPreview_LongPayload_EllipsisesAfterLimit()
    {
        var payload = new byte[64];
        for (var index = 0; index < payload.Length; index++)
        {
            payload[index] = (byte)index;
        }

        var message = CreateMessage(payload);

        var preview = RemoteProcedureCallPayloadFormatter.FormatPreview(message);

        await Assert.That(preview).Contains("…");
    }

    /// <summary>
    ///     The full rendering includes direction, compression flag, length, and a hex dump.
    /// </summary>
    [Test]
    public async Task FormatFull_OutboundMessage_RendersHeaderAndHexDump()
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0x48, 0x49 },
            new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero));

        var rendering = RemoteProcedureCallPayloadFormatter.FormatFull(message);

        await Assert.That(rendering).Contains("Direction : Outbound");
        await Assert.That(rendering).Contains("Compressed: no");
        await Assert.That(rendering).Contains("Length    : 2 bytes");
        await Assert.That(rendering).Contains("48 49");
        await Assert.That(rendering).Contains("HI");
    }

    /// <summary>
    ///     A compressed inbound message surfaces both flags correctly.
    /// </summary>
    [Test]
    public async Task FormatFull_CompressedInboundMessage_RendersCompressedYes()
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Inbound,
            true,
            new byte[] { 0xFF },
            DateTimeOffset.UtcNow);

        var rendering = RemoteProcedureCallPayloadFormatter.FormatFull(message);

        await Assert.That(rendering).Contains("Direction : Inbound");
        await Assert.That(rendering).Contains("Compressed: yes");
    }

    /// <summary>
    ///     The full rendering of an empty payload notes that the payload is empty.
    /// </summary>
    [Test]
    public async Task FormatFull_EmptyPayload_NotesEmpty()
    {
        var message = CreateMessage(Array.Empty<byte>());

        var rendering = RemoteProcedureCallPayloadFormatter.FormatFull(message);

        await Assert.That(rendering).Contains("(empty payload)");
    }

    /// <summary>
    ///     An uncompressed payload that parses as protobuf produces a decoded field tree
    ///     above the raw bytes section.
    /// </summary>
    [Test]
    public async Task FormatFull_UncompressedProtobufPayload_IncludesDecodedFieldTree()
    {
        var payload = new byte[]
        {
            0x08, 0x96, 0x01,
            0x12, 0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F,
        };
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            payload,
            DateTimeOffset.UtcNow);

        var rendering = RemoteProcedureCallPayloadFormatter.FormatFull(message);

        await Assert.That(rendering).Contains("Decoded protobuf:");
        await Assert.That(rendering).Contains("Field 1 (varint): 150");
        await Assert.That(rendering).Contains("Field 2 (string): \"hello\"");
        await Assert.That(rendering).Contains("Raw bytes:");
    }

    /// <summary>
    ///     A compressed payload does not attempt protobuf decoding (Proxyfan does not
    ///     decompress gRPC frames by default).
    /// </summary>
    [Test]
    public async Task FormatFull_CompressedPayload_SkipsProtobufDecoding()
    {
        var payload = new byte[] { 0x08, 0x96, 0x01 };
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Inbound,
            true,
            payload,
            DateTimeOffset.UtcNow);

        var rendering = RemoteProcedureCallPayloadFormatter.FormatFull(message);

        await Assert.That(rendering.Contains("Decoded protobuf:")).IsFalse();
    }

    /// <summary>
    ///     Payloads larger than the detail preview limit have the hex dump truncated and
    ///     surface a "more bytes truncated" notice so the inspector stays responsive.
    /// </summary>
    [Test]
    public async Task FormatFull_OversizedPayload_TruncatesHexDumpWithNotice()
    {
        var payload = new byte[(64 * 1024) + 100];
        var message = CreateMessage(payload);

        var rendering = RemoteProcedureCallPayloadFormatter.FormatFull(message);

        await Assert.That(rendering).Contains("100 more bytes truncated");
        await Assert.That(rendering).Contains("(skipped; payload exceeds preview limit)");
    }

    private static RemoteProcedureCallCapturedMessage CreateMessage(byte[] payload)
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            payload,
            DateTimeOffset.UtcNow);
        return message;
    }
}
