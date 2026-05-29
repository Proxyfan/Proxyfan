using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="WebSocketMessageViewModel" /> covering display projections of
///     direction, opcode, size, timestamp and preview.
/// </summary>
public sealed class WebSocketMessageViewModelTests
{
    /// <summary>
    ///     Verifies that an outbound message renders the up-arrow glyph.
    /// </summary>
    [Test]
    public async Task DirectionGlyph_OutboundMessage_IsUpArrow()
    {
        var message = NewMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, Encoding.UTF8.GetBytes("x"));
        var viewModel = new WebSocketMessageViewModel(message);

        await Assert.That(viewModel.DirectionGlyph).IsEqualTo("↑");
    }

    /// <summary>
    ///     Verifies that an inbound message renders the down-arrow glyph.
    /// </summary>
    [Test]
    public async Task DirectionGlyph_InboundMessage_IsDownArrow()
    {
        var message = NewMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text, Encoding.UTF8.GetBytes("x"));
        var viewModel = new WebSocketMessageViewModel(message);

        await Assert.That(viewModel.DirectionGlyph).IsEqualTo("↓");
    }

    /// <summary>
    ///     Verifies that opcode labels match the expected display strings.
    /// </summary>
    [Test]
    [Arguments(WebSocketOpcode.Text, "Text")]
    [Arguments(WebSocketOpcode.Binary, "Binary")]
    [Arguments(WebSocketOpcode.Ping, "Ping")]
    [Arguments(WebSocketOpcode.Pong, "Pong")]
    [Arguments(WebSocketOpcode.Close, "Close")]
    public async Task OpcodeText_ForOpcode_ReturnsExpectedLabel(WebSocketOpcode opcode, string expected)
    {
        var message = NewMessage(WebSocketDirection.Outbound, opcode, Array.Empty<byte>());
        var viewModel = new WebSocketMessageViewModel(message);

        await Assert.That(viewModel.OpcodeText).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that payload size below 1 KB renders in bytes.
    /// </summary>
    [Test]
    public async Task PayloadSizeText_SmallPayload_RendersInBytes()
    {
        var bytes = new byte[12];
        var message = NewMessage(WebSocketDirection.Outbound, WebSocketOpcode.Binary, bytes);
        var viewModel = new WebSocketMessageViewModel(message);

        await Assert.That(viewModel.PayloadSizeText).IsEqualTo("12 B");
    }

    /// <summary>
    ///     Verifies that payload size between 1 KB and 1 MB renders in kilobytes.
    /// </summary>
    [Test]
    public async Task PayloadSizeText_MediumPayload_RendersInKilobytes()
    {
        var bytes = new byte[2048];
        var message = NewMessage(WebSocketDirection.Outbound, WebSocketOpcode.Binary, bytes);
        var viewModel = new WebSocketMessageViewModel(message);

        await Assert.That(viewModel.PayloadSizeText).IsEqualTo("2.0 KB");
    }

    /// <summary>
    ///     Verifies that payload size above 1 MB renders in megabytes.
    /// </summary>
    [Test]
    public async Task PayloadSizeText_LargePayload_RendersInMegabytes()
    {
        var bytes = new byte[2 * 1024 * 1024];
        var message = NewMessage(WebSocketDirection.Outbound, WebSocketOpcode.Binary, bytes);
        var viewModel = new WebSocketMessageViewModel(message);

        await Assert.That(viewModel.PayloadSizeText).IsEqualTo("2.0 MB");
    }

    /// <summary>
    ///     Verifies that the timestamp text uses the HH:mm:ss.fff format.
    /// </summary>
    [Test]
    public async Task TimestampText_KnownTimestamp_FormatsAsHoursMinutesSecondsFractions()
    {
        var stamp = new DateTimeOffset(2025, 11, 2, 13, 45, 30, 123, TimeSpan.Zero);
        var message = new WebSocketMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, Array.Empty<byte>(), stamp);
        var viewModel = new WebSocketMessageViewModel(message);

        await Assert.That(viewModel.TimestampText).IsEqualTo("13:45:30.123");
    }

    /// <summary>
    ///     Verifies that the wrapped message reference is exposed unchanged.
    /// </summary>
    [Test]
    public async Task Message_AfterConstruction_IsSameReferenceAsInput()
    {
        var message = NewMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, Encoding.UTF8.GetBytes("x"));
        var viewModel = new WebSocketMessageViewModel(message);

        await Assert.That(viewModel.Message).IsSameReferenceAs(message);
    }

    /// <summary>
    ///     Verifies that the preview matches what <see cref="WebSocketPayloadFormatter" /> produces.
    /// </summary>
    [Test]
    public async Task PayloadPreview_TextMessage_MatchesFormatter()
    {
        var message = NewMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, Encoding.UTF8.GetBytes("hello"));
        var viewModel = new WebSocketMessageViewModel(message);

        await Assert.That(viewModel.PayloadPreview).IsEqualTo("hello");
    }

    private static WebSocketMessage NewMessage(WebSocketDirection direction, WebSocketOpcode opcode, byte[] payload)
    {
        return new WebSocketMessage(direction, opcode, payload, DateTimeOffset.UtcNow);
    }
}
