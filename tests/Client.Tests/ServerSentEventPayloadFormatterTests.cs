using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEventPayloadFormatter" />.
/// </summary>
public sealed class ServerSentEventPayloadFormatterTests
{
    /// <summary>
    ///     Verifies that the full-detail rendering includes the type, id, retry hint, capture
    ///     timestamp, and full data field.
    /// </summary>
    [Test]
    public async Task FormatFull_AllFieldsPresent_IncludesEverything()
    {
        var serverSentEvent = new ServerSentEvent(
            "data line one\ndata line two",
            eventType: "message",
            id: "abc",
            retryMilliseconds: 5000,
            timestamp: new DateTimeOffset(2026, 5, 29, 9, 0, 0, TimeSpan.Zero));

        var rendered = ServerSentEventPayloadFormatter.FormatFull(serverSentEvent);

        await Assert.That(rendered).Contains("Captured: 2026-05-29 09:00:00.000");
        await Assert.That(rendered).Contains("Event   : message");
        await Assert.That(rendered).Contains("Id      : abc");
        await Assert.That(rendered).Contains("Retry   : 5000 ms");
        await Assert.That(rendered).Contains("data line one");
        await Assert.That(rendered).Contains("data line two");
    }

    /// <summary>
    ///     Verifies that missing event type and id render placeholders, and the retry line is
    ///     omitted when no retry hint is set.
    /// </summary>
    [Test]
    public async Task FormatFull_OmittedOptionalFields_UsesPlaceholdersAndSkipsRetry()
    {
        var serverSentEvent = new ServerSentEvent("payload", eventType: null, id: null, retryMilliseconds: null, timestamp: DateTimeOffset.UtcNow);

        var rendered = ServerSentEventPayloadFormatter.FormatFull(serverSentEvent);

        await Assert.That(rendered).Contains("Event   : (default)");
        await Assert.That(rendered).Contains("Id      : (none)");
        await Assert.That(rendered).DoesNotContain("Retry   :");
    }

    /// <summary>
    ///     Verifies that the single-line preview collapses newlines to the literal sequence
    ///     <c>" ↵ "</c> for in-table display.
    /// </summary>
    [Test]
    public async Task FormatPreview_DataWithNewlines_CollapsesToLiteralGlyph()
    {
        var serverSentEvent = new ServerSentEvent("alpha\nbeta\r\ngamma", eventType: null, id: null, retryMilliseconds: null, timestamp: DateTimeOffset.UtcNow);

        var preview = ServerSentEventPayloadFormatter.FormatPreview(serverSentEvent);

        await Assert.That(preview).IsEqualTo("alpha ↵ beta ↵ gamma");
    }

    /// <summary>
    ///     Verifies that previews longer than the configured limit are truncated with an
    ///     ellipsis.
    /// </summary>
    [Test]
    public async Task FormatPreview_DataLongerThanLimit_TruncatesWithEllipsis()
    {
        var longData = new string('x', 200);
        var serverSentEvent = new ServerSentEvent(longData, eventType: null, id: null, retryMilliseconds: null, timestamp: DateTimeOffset.UtcNow);

        var preview = ServerSentEventPayloadFormatter.FormatPreview(serverSentEvent);

        await Assert.That(preview.EndsWith('…')).IsTrue();
        await Assert.That(preview.Count(character => character == 'x')).IsEqualTo(120);
    }
}
