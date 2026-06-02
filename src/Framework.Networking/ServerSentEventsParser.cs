using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parser for Server-Sent Events (text/event-stream) byte streams. The parser supports
///     incremental ingestion — call <see cref="Append" /> with each newly-received chunk and
///     drain completed events via <see cref="DrainCompletedEvents" />.
/// </summary>
public sealed class ServerSentEventsParser
{
    private readonly StringBuilder _carry;
    private readonly List<ServerSentEvent> _completed;
    private readonly StringBuilder _data;
    private readonly Decoder _decoder;
    private string? _eventType;
    private string? _id;
    private int? _retry;

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEventsParser" /> with no pending state.
    /// </summary>
    public ServerSentEventsParser()
    {
        var carry = new StringBuilder();
        var completed = new List<ServerSentEvent>();
        var data = new StringBuilder();
        _carry = carry;
        _completed = completed;
        _data = data;
        _decoder = Encoding.UTF8.GetDecoder();
    }

    /// <summary>
    ///     Feeds the next chunk of UTF-8 bytes into the parser.
    /// </summary>
    /// <param name="chunk">The chunk of bytes (decoded as UTF-8).</param>
    /// <param name="timestamp">The timestamp to assign to events finalized in this call.</param>
    public void Append(ReadOnlyMemory<byte> chunk, DateTimeOffset timestamp)
    {
        var span = chunk.Span;
        var charCount = _decoder.GetCharCount(span, flush: false);
        if (charCount > 0)
        {
            var buffer = new char[charCount];
            var written = _decoder.GetChars(span, buffer, flush: false);
            _carry.Append(buffer, 0, written);
        }

        while (HasNextLine(out var line))
        {
            ProcessLine(line, timestamp);
        }
    }

    /// <summary>
    ///     Signals end-of-stream to the parser. Flushes any buffered partial UTF-8 bytes
    ///     (surfacing them as U+FFFD if incomplete) and drains any newly-completed lines
    ///     that the flushed characters terminate. Completed events are available via
    ///     <see cref="DrainCompletedEvents" />.
    /// </summary>
    /// <param name="timestamp">The timestamp to assign to events finalized in this call.</param>
    public void Complete(DateTimeOffset timestamp)
    {
        ReadOnlySpan<byte> empty = [];
        var charCount = _decoder.GetCharCount(empty, flush: true);
        if (charCount > 0)
        {
            var buffer = new char[charCount];
            var written = _decoder.GetChars(empty, buffer, flush: true);
            _carry.Append(buffer, 0, written);
        }

        while (HasNextLine(out var line))
        {
            ProcessLine(line, timestamp);
        }
    }

    /// <summary>
    ///     Removes and returns all completed events accumulated so far.
    /// </summary>
    /// <returns>The drained events.</returns>
    public IReadOnlyList<ServerSentEvent> DrainCompletedEvents()
    {
        var snapshot = _completed.ToArray();
        _completed.Clear();
        return snapshot;
    }

    private void ApplyField(ServerSentEventField field)
    {
        switch (field.Name)
        {
            case "data":
                if (_data.Length > 0)
                {
                    _data.Append('\n');
                }
                _data.Append(field.Value);
                break;
            case "event":
                _eventType = field.Value;
                break;
            case "id":
                _id = field.Value;
                break;
            case "retry":
                if (int.TryParse(field.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var retry) && retry >= 0)
                {
                    _retry = retry;
                }
                break;
            default:
                break;
        }
    }

    private void FinalizeEvent(DateTimeOffset timestamp)
    {
        var dataText = _data.ToString();
        _data.Clear();
        var hasContent = dataText.Length > 0 || _eventType is not null || _id is not null || _retry is not null;
        if (!hasContent)
        {
            return;
        }

        var sse = new ServerSentEvent(dataText, _eventType, _id, _retry, timestamp);
        _completed.Add(sse);
        _eventType = null;
        _id = null;
        _retry = null;
    }

    private bool HasNextLine(out string line)
    {
        var carryString = _carry.ToString();
        var lineEnd = -1;
        var lineEndLength = 0;

        for (var index = 0; index < carryString.Length; index++)
        {
            if (carryString[index] == '\r')
            {
                lineEnd = index;
                lineEndLength = index + 1 < carryString.Length && carryString[index + 1] == '\n' ? 2 : 1;
                break;
            }

            if (carryString[index] == '\n')
            {
                lineEnd = index;
                lineEndLength = 1;
                break;
            }
        }

        if (lineEnd < 0)
        {
            line = string.Empty;
            return false;
        }

        line = carryString[..lineEnd];
        _carry.Clear();
        _carry.Append(carryString[(lineEnd + lineEndLength)..]);
        return true;
    }

    private void ProcessLine(string line, DateTimeOffset timestamp)
    {
        if (line.Length == 0)
        {
            FinalizeEvent(timestamp);
            return;
        }

        var field = ServerSentEventsLineParser.ParseField(line);

        if (field is null)
        {
            return;
        }

        ApplyField(field);
    }
}
