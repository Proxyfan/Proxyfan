using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="WebSocketPayloadFormatter" /> covering preview and full
///     payload rendering across opcodes, encodings and edge cases.
/// </summary>
public sealed class WebSocketPayloadFormatterTests
{
    /// <summary>
    ///     Verifies that an empty text payload returns an empty string.
    /// </summary>
    [Test]
    public async Task FormatFull_EmptyTextPayload_ReturnsEmpty()
    {
        var message = NewMessage(WebSocketOpcode.Text, Array.Empty<byte>());

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a plain text payload is decoded as UTF-8.
    /// </summary>
    [Test]
    public async Task FormatFull_PlainTextPayload_DecodesAsUtf8()
    {
        var message = NewMessage(WebSocketOpcode.Text, Encoding.UTF8.GetBytes("hello"));

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that a JSON object text payload is pretty-printed.
    /// </summary>
    [Test]
    public async Task FormatFull_JsonObjectPayload_PrettyPrints()
    {
        var message = NewMessage(WebSocketOpcode.Text, Encoding.UTF8.GetBytes("{\"a\":1,\"b\":2}"));

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result.Contains("\"a\": 1")).IsTrue();
        await Assert.That(result.Contains("\"b\": 2")).IsTrue();
    }

    /// <summary>
    ///     Verifies that a JSON array text payload is pretty-printed.
    /// </summary>
    [Test]
    public async Task FormatFull_JsonArrayPayload_PrettyPrints()
    {
        var message = NewMessage(WebSocketOpcode.Text, Encoding.UTF8.GetBytes("[1,2,3]"));

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result.Contains('\n')).IsTrue();
        await Assert.That(result.Contains('1')).IsTrue();
    }

    /// <summary>
    ///     Verifies that text starting with <c>{</c> but containing malformed JSON falls
    ///     back to the raw decoded text.
    /// </summary>
    [Test]
    public async Task FormatFull_MalformedJsonPayload_ReturnsRawText()
    {
        var message = NewMessage(WebSocketOpcode.Text, Encoding.UTF8.GetBytes("{not json"));

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result).IsEqualTo("{not json");
    }

    /// <summary>
    ///     Verifies that text not starting with a JSON token is returned verbatim.
    /// </summary>
    [Test]
    public async Task FormatFull_NonJsonText_ReturnsVerbatim()
    {
        var message = NewMessage(WebSocketOpcode.Text, Encoding.UTF8.GetBytes("plain message"));

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result).IsEqualTo("plain message");
    }

    /// <summary>
    ///     Verifies that whitespace-only text bypasses pretty-print and returns verbatim.
    /// </summary>
    [Test]
    public async Task FormatFull_WhitespaceOnlyText_ReturnsVerbatim()
    {
        var message = NewMessage(WebSocketOpcode.Text, Encoding.UTF8.GetBytes("   "));

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result).IsEqualTo("   ");
    }

    /// <summary>
    ///     Verifies that a binary payload is rendered as a hex dump with offset prefix.
    /// </summary>
    [Test]
    public async Task FormatFull_BinaryPayload_RendersHexDump()
    {
        var bytes = new byte[] { 0x00, 0xFF, 0x42, 0x10 };
        var message = NewMessage(WebSocketOpcode.Binary, bytes);

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result.Contains("00000000")).IsTrue();
        await Assert.That(result.Contains("00 FF 42 10")).IsTrue();
    }

    /// <summary>
    ///     Verifies that the hex dump emits multiple rows when the payload exceeds 16 bytes.
    /// </summary>
    [Test]
    public async Task FormatFull_BinaryPayloadLongerThanRow_RendersMultipleRows()
    {
        var bytes = new byte[32];
        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = (byte)index;
        }

        var message = NewMessage(WebSocketOpcode.Binary, bytes);

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result.Contains("00000000")).IsTrue();
        await Assert.That(result.Contains("00000010")).IsTrue();
    }

    /// <summary>
    ///     Verifies that a control frame (Close/Ping/Pong) with a payload renders as a hex dump.
    /// </summary>
    [Test]
    public async Task FormatFull_CloseFrameWithPayload_RendersHexDump()
    {
        var bytes = new byte[] { 0x03, 0xE8, 0x62, 0x79, 0x65 };
        var message = NewMessage(WebSocketOpcode.Close, bytes);

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result.Contains("03 E8 62 79 65")).IsTrue();
    }

    /// <summary>
    ///     Verifies that the hex dump ASCII column renders printable ASCII as glyphs and
    ///     non-printable bytes as a placeholder.
    /// </summary>
    [Test]
    public async Task FormatFull_BinaryPayloadPrintableAndNonPrintable_RendersAsciiColumn()
    {
        var bytes = new byte[] { (byte)'H', (byte)'i', 0x01, 0x7F };
        var message = NewMessage(WebSocketOpcode.Binary, bytes);

        var result = WebSocketPayloadFormatter.FormatFull(message);

        await Assert.That(result.Contains("Hi..")).IsTrue();
    }

    /// <summary>
    ///     Verifies that a null message is handled defensively and yields an empty string.
    /// </summary>
    [Test]
    public async Task FormatFull_NullMessage_ReturnsEmpty()
    {
        var result = WebSocketPayloadFormatter.FormatFull(null!);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a text preview escapes whitespace glyphs.
    /// </summary>
    [Test]
    public async Task FormatPreview_TextWithControlCharacters_ReplacesWithGlyphs()
    {
        var message = NewMessage(WebSocketOpcode.Text, Encoding.UTF8.GetBytes("a\r\nb\tc"));

        var preview = WebSocketPayloadFormatter.FormatPreview(message);

        await Assert.That(preview).IsEqualTo("a␍␊b␉c");
    }

    /// <summary>
    ///     Verifies that long text previews are truncated to 80 characters with an ellipsis.
    /// </summary>
    [Test]
    public async Task FormatPreview_LongText_TruncatesToEighty()
    {
        var longText = new string('a', 200);
        var message = NewMessage(WebSocketOpcode.Text, Encoding.UTF8.GetBytes(longText));

        var preview = WebSocketPayloadFormatter.FormatPreview(message);

        await Assert.That(preview.Length).IsEqualTo(81);
        await Assert.That(preview.EndsWith('…')).IsTrue();
    }

    /// <summary>
    ///     Verifies that a binary preview reports payload size.
    /// </summary>
    [Test]
    public async Task FormatPreview_BinaryMultipleBytes_ReportsByteCount()
    {
        var message = NewMessage(WebSocketOpcode.Binary, new byte[] { 1, 2, 3 });

        var preview = WebSocketPayloadFormatter.FormatPreview(message);

        await Assert.That(preview).IsEqualTo("<3 bytes>");
    }

    /// <summary>
    ///     Verifies that a single-byte binary preview uses the singular form.
    /// </summary>
    [Test]
    public async Task FormatPreview_BinarySingleByte_UsesSingularByteForm()
    {
        var message = NewMessage(WebSocketOpcode.Binary, new byte[] { 1 });

        var preview = WebSocketPayloadFormatter.FormatPreview(message);

        await Assert.That(preview).IsEqualTo("<1 byte>");
    }

    /// <summary>
    ///     Verifies that an empty control frame preview returns an empty string.
    /// </summary>
    [Test]
    public async Task FormatPreview_EmptyControlFrame_ReturnsEmpty()
    {
        var message = NewMessage(WebSocketOpcode.Ping, Array.Empty<byte>());

        var preview = WebSocketPayloadFormatter.FormatPreview(message);

        await Assert.That(preview).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a control frame with a payload renders as a byte-count preview.
    /// </summary>
    [Test]
    public async Task FormatPreview_ControlFrameWithPayload_ReportsByteCount()
    {
        var message = NewMessage(WebSocketOpcode.Pong, new byte[] { 1, 2 });

        var preview = WebSocketPayloadFormatter.FormatPreview(message);

        await Assert.That(preview).IsEqualTo("<2 bytes>");
    }

    /// <summary>
    ///     Verifies that a null message preview yields empty string.
    /// </summary>
    [Test]
    public async Task FormatPreview_NullMessage_ReturnsEmpty()
    {
        var preview = WebSocketPayloadFormatter.FormatPreview(null!);

        await Assert.That(preview).IsEqualTo(string.Empty);
    }

    private static WebSocketMessage NewMessage(WebSocketOpcode opcode, byte[] payload)
    {
        return new WebSocketMessage(WebSocketDirection.Inbound, opcode, payload, DateTimeOffset.UtcNow);
    }
}
