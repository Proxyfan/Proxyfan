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
        var requestBody = LineDiffer.Diff(FormatBody(oldFlow.Request?.Body), FormatBody(newFlow.Request?.Body));
        var responseBody = LineDiffer.Diff(FormatBody(oldFlow.Response?.Body), FormatBody(newFlow.Response?.Body));

        var isIdentical = HasNoChanges(url)
                          && HasNoChanges(method)
                          && HasNoChanges(status)
                          && HasNoChanges(requestHeaders)
                          && HasNoChanges(responseHeaders)
                          && HasNoChanges(requestBody)
                          && HasNoChanges(responseBody);

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

    private static string FormatBody(ReadOnlyMemory<byte>? body)
    {
        if (body is null or { Length: 0 })
        {
            return string.Empty;
        }

        var span = body.Value.Span;
        if (span.Length > MaximumDiffableBodyLength)
        {
            return $"<binary or oversized body, {span.Length} bytes>";
        }

        if (!HasOnlyPrintableBytes(span))
        {
            return $"<binary body, {span.Length} bytes>";
        }

        return Encoding.UTF8.GetString(span);
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

    private static int HeaderEntryComparer(KeyValuePair<string, string[]> left, KeyValuePair<string, string[]> right)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key);
    }
}
