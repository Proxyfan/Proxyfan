using System;
using System.Collections.Generic;
using System.Text;

namespace Proxyfan.Domain.Traffic.Diff;

/// <summary>
///     Produces a structured <see cref="TrafficFlowDiff" /> between two
///     <see cref="TrafficFlow" /> instances by diffing each major section
///     (URL, method, status, headers, body) independently.
/// </summary>
public static class TrafficFlowDiffer
{
    /// <summary>
    ///     The maximum body size, in bytes, that will be diffed line-by-line. Bodies
    ///     larger than this are diffed as a single synthetic line containing only the
    ///     length to keep the comparison fast and avoid unbounded memory use.
    /// </summary>
    public const int MaximumDiffableBodyLength = 256 * 1024;

    /// <summary>
    ///     The maximum number of lines in a text body that may be sent to line-by-line
    ///     diffing.
    /// </summary>
    public const int MaximumDiffableBodyLineCount = 4096;
    private const long MaximumDiffableBodyDiffWork = 1_000_000;

    /// <summary>
    ///     Computes a section-by-section diff of <paramref name="oldFlow" /> and
    ///     <paramref name="newFlow" />. Either input may have a missing request or
    ///     response; absent sections are diffed against the empty string.
    /// </summary>
    /// <param name="oldFlow">The flow on the left-hand side of the diff.</param>
    /// <param name="newFlow">The flow on the right-hand side of the diff.</param>
    /// <returns>
    ///     A populated <see cref="TrafficFlowDiff" /> with one segment list per section.
    /// </returns>
    public static TrafficFlowDiff Diff(TrafficFlow oldFlow, TrafficFlow newFlow)
    {
        var url = LineDiffer.Diff(oldFlow.Request?.RequestUri.ToString(), newFlow.Request?.RequestUri.ToString());
        var method = LineDiffer.Diff(oldFlow.Request?.Method, newFlow.Request?.Method);
        var status = LineDiffer.Diff(FormatStatus(oldFlow.Response), FormatStatus(newFlow.Response));
        var requestHeaders = LineDiffer.Diff(FormatHeaders(oldFlow.Request?.Headers), FormatHeaders(newFlow.Request?.Headers));
        var responseHeaders = LineDiffer.Diff(FormatHeaders(oldFlow.Response?.Headers), FormatHeaders(newFlow.Response?.Headers));
        var oldRequestBody = oldFlow.Request?.Body;
        var newRequestBody = newFlow.Request?.Body;
        var oldResponseBody = oldFlow.Response?.Body;
        var newResponseBody = newFlow.Response?.Body;
        var requestBody = DiffBody(oldRequestBody, newRequestBody);
        var responseBody = DiffBody(oldResponseBody, newResponseBody);

        var isIdentical = HasNoChanges(url)
                          && HasNoChanges(method)
                          && HasNoChanges(status)
                          && HasNoChanges(requestHeaders)
                          && HasNoChanges(responseHeaders)
                          && HasEquivalentBodies(oldRequestBody, newRequestBody)
                          && HasEquivalentBodies(oldResponseBody, newResponseBody);

        var diff = new TrafficFlowDiff
        {
            IsIdentical = isIdentical,
            Method = method,
            RequestBody = requestBody,
            RequestHeaders = requestHeaders,
            ResponseBody = responseBody,
            ResponseHeaders = responseHeaders,
            Status = status,
            Url = url,
        };
        return diff;
    }

    private static int CountLines(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return 0;
        }

        var lineCount = 1;
        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] == '\n')
            {
                lineCount++;
            }
        }

        return lineCount;
    }

    private static BodyDiffInput CreateBodyDiffInput(ReadOnlyMemory<byte>? body)
    {
        if (body is null or { Length: 0 })
        {
            return new BodyDiffInput
            {
                ByteLength = 0,
                IsText = true,
                LineCount = 0,
                Text = string.Empty,
            };
        }

        var span = body.Value.Span;
        if (span.Length > MaximumDiffableBodyLength)
        {
            return new BodyDiffInput
            {
                ByteLength = span.Length,
                IsText = false,
                LineCount = 1,
                Text = $"<binary or oversized body, {span.Length} bytes>",
            };
        }

        if (!HasOnlyPrintableBytes(span))
        {
            return new BodyDiffInput
            {
                ByteLength = span.Length,
                IsText = false,
                LineCount = 1,
                Text = $"<binary body, {span.Length} bytes>",
            };
        }

        return new BodyDiffInput
        {
            ByteLength = span.Length,
            IsText = true,
            LineCount = CountLines(span),
            Text = Encoding.UTF8.GetString(span),
        };
    }

    private static IReadOnlyList<LineDiffSegment> DiffBody(ReadOnlyMemory<byte>? oldBody, ReadOnlyMemory<byte>? newBody)
    {
        var oldInput = CreateBodyDiffInput(oldBody);
        var newInput = CreateBodyDiffInput(newBody);

        if (HasTooManyLinesSummary(oldInput, newInput))
        {
            oldInput = oldInput with
            {
                Text = FormatTooManyLinesBody(oldInput.ByteLength, oldInput.LineCount),
            };
            newInput = newInput with
            {
                Text = FormatTooManyLinesBody(newInput.ByteLength, newInput.LineCount),
            };
        }

        return LineDiffer.Diff(oldInput.Text, newInput.Text);
    }

    private static string FormatHeaders(HeaderCollection? headers)
    {
        if (headers is null or { Count: 0 })
        {
            return string.Empty;
        }

        var entries = new KeyValuePair<string, string[]>[headers.Count];
        var entryIndex = 0;
        foreach (var entry in headers)
        {
            entries[entryIndex] = entry;
            entryIndex++;
        }

        Array.Sort(entries, HeaderEntryComparer);

        var builder = new StringBuilder();
        var first = true;
        foreach (var entry in entries)
        {
            foreach (var value in entry.Value)
            {
                if (!first)
                {
                    builder.Append('\n');
                }

                builder.Append(entry.Key);
                builder.Append(": ");
                builder.Append(value);
                first = false;
            }
        }

        return builder.ToString();
    }

    private static string FormatStatus(HypertextTransferProtocolResponseData? response)
    {
        if (response is null)
        {
            return string.Empty;
        }

        return $"{response.StatusCode} {response.ReasonPhrase}";
    }

    private static string FormatTooManyLinesBody(int byteLength, int lineCount)
    {
        return $"<text body omitted: too many lines ({lineCount} lines, {byteLength} bytes)>";
    }

    private static bool HasEquivalentBodies(ReadOnlyMemory<byte>? left, ReadOnlyMemory<byte>? right)
    {
        if (left is null or { Length: 0 })
        {
            return right is null or { Length: 0 };
        }

        if (right is null || left.Value.Length != right.Value.Length)
        {
            return false;
        }

        return left.Value.Span.SequenceEqual(right.Value.Span);
    }

    private static bool HasNoChanges(IReadOnlyList<LineDiffSegment> segments)
    {
        for (var index = 0; index < segments.Count; index++)
        {
            if (segments[index].Operation != LineDiffOperation.Equal)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasOnlyPrintableBytes(ReadOnlySpan<byte> bytes)
    {
        for (var index = 0; index < bytes.Length; index++)
        {
            var value = bytes[index];
            if (value == 0)
            {
                return false;
            }

            if (value < 0x09)
            {
                return false;
            }

            if (value is > 0x0D and < 0x20)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasTooManyLinesSummary(BodyDiffInput oldInput, BodyDiffInput newInput)
    {
        if (oldInput.IsText && oldInput.LineCount > MaximumDiffableBodyLineCount)
        {
            return true;
        }

        if (newInput.IsText && newInput.LineCount > MaximumDiffableBodyLineCount)
        {
            return true;
        }

        if (!oldInput.IsText || !newInput.IsText)
        {
            return false;
        }

        var estimatedDiffWork = (long)oldInput.LineCount * newInput.LineCount;
        return estimatedDiffWork > MaximumDiffableBodyDiffWork;
    }

    private static int HeaderEntryComparer(KeyValuePair<string, string[]> left, KeyValuePair<string, string[]> right)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key);
    }

    private readonly record struct BodyDiffInput
    {
        public int ByteLength { get; init; }

        public bool IsText { get; init; }

        public int LineCount { get; init; }

        public required string Text { get; init; }
    }
}
