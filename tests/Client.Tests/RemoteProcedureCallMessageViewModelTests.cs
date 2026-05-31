using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallMessageViewModel" />.
/// </summary>
public sealed class RemoteProcedureCallMessageViewModelTests
{
    /// <summary>
    ///     Outbound messages use the upward arrow glyph.
    /// </summary>
    [Test]
    public async Task DirectionGlyph_OutboundMessage_UsesUpArrow()
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0x01 },
            DateTimeOffset.UtcNow);

        var viewModel = new RemoteProcedureCallMessageViewModel(message);

        await Assert.That(viewModel.DirectionGlyph).IsEqualTo("↑");
    }

    /// <summary>
    ///     Inbound messages use the downward arrow glyph.
    /// </summary>
    [Test]
    public async Task DirectionGlyph_InboundMessage_UsesDownArrow()
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Inbound,
            false,
            new byte[] { 0x01 },
            DateTimeOffset.UtcNow);

        var viewModel = new RemoteProcedureCallMessageViewModel(message);

        await Assert.That(viewModel.DirectionGlyph).IsEqualTo("↓");
    }

    /// <summary>
    ///     Compressed messages surface a non-empty compression flag text.
    /// </summary>
    [Test]
    public async Task CompressionText_CompressedMessage_IsYes()
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            true,
            new byte[] { 0x01 },
            DateTimeOffset.UtcNow);

        var viewModel = new RemoteProcedureCallMessageViewModel(message);

        await Assert.That(viewModel.CompressionText).IsEqualTo("yes");
    }

    /// <summary>
    ///     Uncompressed messages render an empty compression flag text.
    /// </summary>
    [Test]
    public async Task CompressionText_UncompressedMessage_IsEmpty()
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0x01 },
            DateTimeOffset.UtcNow);

        var viewModel = new RemoteProcedureCallMessageViewModel(message);

        await Assert.That(viewModel.CompressionText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     The timestamp is formatted as <c>HH:mm:ss.fff</c>.
    /// </summary>
    [Test]
    public async Task TimestampText_FormattedAsClock_MatchesPattern()
    {
        var timestamp = new DateTimeOffset(2024, 1, 1, 12, 34, 56, 789, TimeSpan.Zero);
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0x01 },
            timestamp);

        var viewModel = new RemoteProcedureCallMessageViewModel(message);

        await Assert.That(viewModel.TimestampText).IsEqualTo("12:34:56.789");
    }

    /// <summary>
    ///     The captured message reference is preserved.
    /// </summary>
    [Test]
    public async Task CapturedMessage_ReferenceIsExposed_IsSameReference()
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0x01 },
            DateTimeOffset.UtcNow);

        var viewModel = new RemoteProcedureCallMessageViewModel(message);

        await Assert.That(viewModel.CapturedMessage).IsSameReferenceAs(message);
    }

    /// <summary>
    ///     The payload preview surfaces hex bytes.
    /// </summary>
    [Test]
    public async Task PayloadPreview_IncludesHexBytes_IsNonEmpty()
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0xAB, 0xCD },
            DateTimeOffset.UtcNow);

        var viewModel = new RemoteProcedureCallMessageViewModel(message);

        await Assert.That(viewModel.PayloadPreview).IsEqualTo("AB CD");
    }

    /// <summary>
    ///     The size text uses the shared byte-size formatter.
    /// </summary>
    [Test]
    public async Task SizeText_PayloadOfTwoBytes_RendersByteCount()
    {
        var message = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0xAB, 0xCD },
            DateTimeOffset.UtcNow);

        var viewModel = new RemoteProcedureCallMessageViewModel(message);

        await Assert.That(viewModel.SizeText).Contains("2");
    }
}
