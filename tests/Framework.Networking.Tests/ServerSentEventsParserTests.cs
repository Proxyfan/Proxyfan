using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEventsParser" /> and
///     <see cref="ServerSentEventsLineParser" />.
/// </summary>
public sealed class ServerSentEventsParserTests
{
    /// <summary>
    ///     Verifies that a single data event terminated by a blank line is parsed.
    /// </summary>
    [Test]
    public async Task Append_SingleDataEvent_ParsesPayload()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes("data: hello\n\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that multi-line data fields are joined with a newline.
    /// </summary>
    [Test]
    public async Task Append_MultiLineDataField_JoinsWithNewline()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes("data: line1\ndata: line2\n\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("line1\nline2");
    }

    /// <summary>
    ///     Verifies that event:, id:, and retry: fields are captured.
    /// </summary>
    [Test]
    public async Task Append_EventIdAndRetry_AreCaptured()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes("event: update\nid: 42\nretry: 5000\ndata: hello\n\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events[0].EventType).IsEqualTo("update");
        await Assert.That(events[0].Id).IsEqualTo("42");
        await Assert.That(events[0].RetryMilliseconds).IsEqualTo(5000);
    }

    /// <summary>
    ///     Verifies that comment lines (starting with :) are ignored.
    /// </summary>
    [Test]
    public async Task Append_CommentLine_IsIgnored()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes(": keepalive\ndata: payload\n\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("payload");
    }

    /// <summary>
    ///     Verifies that CRLF line endings are honored alongside LF.
    /// </summary>
    [Test]
    public async Task Append_CarriageReturnLineFeed_ParsesEvent()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes("data: crlf\r\n\r\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("crlf");
    }

    /// <summary>
    ///     Verifies that incremental chunks are buffered until the event terminator arrives.
    /// </summary>
    [Test]
    public async Task Append_ChunkedDelivery_BuffersUntilBlankLine()
    {
        var parser = new ServerSentEventsParser();
        parser.Append(Encoding.UTF8.GetBytes("data: par"), DateTimeOffset.UtcNow);
        var firstDrain = parser.DrainCompletedEvents();
        parser.Append(Encoding.UTF8.GetBytes("t1\n\n"), DateTimeOffset.UtcNow);
        var secondDrain = parser.DrainCompletedEvents();

        await Assert.That(firstDrain.Count).IsEqualTo(0);
        await Assert.That(secondDrain.Count).IsEqualTo(1);
        await Assert.That(secondDrain[0].Data).IsEqualTo("part1");
    }

    /// <summary>
    ///     Verifies that an invalid retry value (negative) is silently dropped.
    /// </summary>
    [Test]
    public async Task Append_NegativeRetry_IsDropped()
    {
        var parser = new ServerSentEventsParser();
        parser.Append(Encoding.UTF8.GetBytes("retry: -5\ndata: x\n\n"), DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events[0].RetryMilliseconds).IsNull();
    }

    /// <summary>
    ///     Verifies that an invalid retry value (non-numeric) is silently dropped.
    /// </summary>
    [Test]
    public async Task Append_NonNumericRetry_IsDropped()
    {
        var parser = new ServerSentEventsParser();
        parser.Append(Encoding.UTF8.GetBytes("retry: abc\ndata: x\n\n"), DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events[0].RetryMilliseconds).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsLineParser.ParseField(string)" /> returns
    ///     null for empty lines and comment lines.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments(": comment")]
    public async Task ParseField_EmptyOrComment_ReturnsNull(string line)
    {
        var field = ServerSentEventsLineParser.ParseField(line);

        await Assert.That(field).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsLineParser.ParseField(string)" /> returns
    ///     a field-only result when no colon is present.
    /// </summary>
    [Test]
    public async Task ParseField_NoColon_ReturnsFieldOnly()
    {
        var field = ServerSentEventsLineParser.ParseField("data");

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.Name).IsEqualTo("data");
        await Assert.That(field.Value).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a leading space after the colon is stripped per the spec.
    /// </summary>
    [Test]
    public async Task ParseField_LeadingSpaceAfterColon_IsStripped()
    {
        var field = ServerSentEventsLineParser.ParseField("data: hello");

        await Assert.That(field!.Value).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that two separate events are emitted when separated by a blank line.
    /// </summary>
    [Test]
    public async Task Append_TwoEvents_EmitsBoth()
    {
        var parser = new ServerSentEventsParser();
        parser.Append(Encoding.UTF8.GetBytes("data: one\n\ndata: two\n\n"), DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(2);
        await Assert.That(events[0].Data).IsEqualTo("one");
        await Assert.That(events[1].Data).IsEqualTo("two");
    }

    /// <summary>
    ///     Verifies that an empty blank-line-only stream produces no events.
    /// </summary>
    [Test]
    public async Task Append_BlankLineOnly_ProducesNoEvents()
    {
        var parser = new ServerSentEventsParser();
        parser.Append(Encoding.UTF8.GetBytes("\n\n\n"), DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsLineParser.ParseField(string)" /> returns
    ///     a field with no value when the colon has no value after it.
    /// </summary>
    [Test]
    public async Task ParseField_ColonWithoutValue_ReturnsEmptyValue()
    {
        var field = ServerSentEventsLineParser.ParseField("data:");

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.Name).IsEqualTo("data");
        await Assert.That(field.Value).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsLineParser.ParseField(string)" /> with
    ///     no leading space after colon preserves the value verbatim.
    /// </summary>
    [Test]
    public async Task ParseField_NoLeadingSpaceAfterColon_PreservesValue()
    {
        var field = ServerSentEventsLineParser.ParseField("data:hello");

        await Assert.That(field!.Value).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that an unknown field name (other than data/event/id/retry) is silently
    ///     ignored (covers the default switch arm of ApplyField).
    /// </summary>
    [Test]
    public async Task Append_UnknownFieldName_IsSilentlyIgnored()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes("x-custom-field: ignored\ndata: payload\n\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("payload");
    }

    /// <summary>
    ///     Verifies that multibyte UTF-8 scalars split across three chunks (with splits
    ///     inside both a 2-byte and a 4-byte scalar) decode intact rather than producing
    ///     replacement characters.
    /// </summary>
    [Test]
    public async Task Append_MultibyteUtf8SplitAcrossChunks_DecodesIntact()
    {
        var parser = new ServerSentEventsParser();
        // "data: héllo🌍\n\n" — 'é' is 2 bytes (0xC3 0xA9), '🌍' is 4 bytes (0xF0 0x9F 0x8C 0x8D).
        var bytes = Encoding.UTF8.GetBytes("data: héllo🌍\n\n");

        // Split inside the 'é' (between 0xC3 and 0xA9) and inside the '🌍' (after 2 of its 4 bytes).
        var firstSplit = 8; // "data: h" + 0xC3 (first byte of 'é')
        var secondSplit = 14; // through "data: héllo" + 0xF0 0x9F (first 2 bytes of '🌍')

        parser.Append(bytes.AsMemory(0, firstSplit), DateTimeOffset.UtcNow);
        parser.Append(bytes.AsMemory(firstSplit, secondSplit - firstSplit), DateTimeOffset.UtcNow);
        parser.Append(bytes.AsMemory(secondSplit), DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("héllo🌍");
    }

    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsParser.Complete" /> can be called at
    ///     end-of-stream when the decoder still has a buffered incomplete multibyte sequence,
    ///     flushing it without throwing or producing spurious events.
    /// </summary>
    [Test]
    public async Task Complete_IncompleteMultibyteAtEndOfStream_FlushesWithoutThrowing()
    {
        var parser = new ServerSentEventsParser();
        var prefix = Encoding.UTF8.GetBytes("data: hello\n\n");
        var truncatedLead = new byte[] { 0xC3 }; // lone lead byte of a 2-byte UTF-8 scalar

        parser.Append(prefix, DateTimeOffset.UtcNow);
        parser.Append(truncatedLead, DateTimeOffset.UtcNow);
        parser.Complete(DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that an id-only block (no data) does not dispatch a spurious empty
    ///     event, but the id is preserved and attached to the next real event per the
    ///     SSE last-event-id semantics.
    /// </summary>
    [Test]
    public async Task Append_IdOnlyBlock_DoesNotDispatchAndPreservesId()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes("id: 7\n\ndata: hello\n\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("hello");
        await Assert.That(events[0].Id).IsEqualTo("7");
    }

    /// <summary>
    ///     Verifies that a retry-only block does not dispatch a spurious empty event and
    ///     does not carry the retry value forward to a subsequent event.
    /// </summary>
    [Test]
    public async Task Append_RetryOnlyBlock_DoesNotDispatch()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes("retry: 1500\n\ndata: hello\n\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("hello");
        await Assert.That(events[0].RetryMilliseconds).IsNull();
    }

    /// <summary>
    ///     Verifies that an event-only block (no data) does not dispatch a spurious empty
    ///     event and that the event-type buffer is reset so it does not bleed into the
    ///     next dispatched event.
    /// </summary>
    [Test]
    public async Task Append_EventOnlyBlock_DoesNotDispatchAndResetsType()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes("event: ping\n\ndata: hello\n\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Data).IsEqualTo("hello");
        await Assert.That(events[0].EventType).IsNull();
    }

    /// <summary>
    ///     Verifies that the last-event-id state persists across dispatched events even
    ///     when subsequent events omit the id field, per the SSE spec.
    /// </summary>
    [Test]
    public async Task Append_OmittedId_InheritsPreviousId()
    {
        var parser = new ServerSentEventsParser();
        var bytes = Encoding.UTF8.GetBytes("id: 42\ndata: first\n\ndata: second\n\n");

        parser.Append(bytes, DateTimeOffset.UtcNow);
        var events = parser.DrainCompletedEvents();

        await Assert.That(events.Count).IsEqualTo(2);
        await Assert.That(events[0].Id).IsEqualTo("42");
        await Assert.That(events[1].Id).IsEqualTo("42");
    }
}
