using System.Collections.Generic;
using System.Text;

namespace Proxyfan.Domain.Traffic.Diff;

/// <summary>
///     Renders a <see cref="TrafficFlowDiff" /> as a unified-diff style plain-text
///     document, suitable for copying to the clipboard or saving to a file.
/// </summary>
public static class UnifiedDiffFormatter
{
    /// <summary>
    ///     Formats <paramref name="diff" /> as a multi-section unified diff. Each
    ///     non-empty section is rendered with a `--- old / +++ new` style header
    ///     followed by per-line markers (<c>' '</c> equal, <c>'+'</c> insert,
    ///     <c>'-'</c> delete).
    /// </summary>
    /// <param name="diff">The structured flow diff to render.</param>
    /// <returns>
    ///     The unified-diff text. Returns a single "no differences" line when
    ///     <paramref name="diff" /> reports no changes.
    /// </returns>
    public static string Format(TrafficFlowDiff diff)
    {
        if (diff.IsIdentical)
        {
            return "(no differences)";
        }

        var builder = new StringBuilder();
        AppendSection(builder, "URL", diff.Url);
        AppendSection(builder, "Method", diff.Method);
        AppendSection(builder, "Status", diff.Status);
        AppendSection(builder, "Request Headers", diff.RequestHeaders);
        AppendSection(builder, "Request Body", diff.RequestBody);
        AppendSection(builder, "Response Headers", diff.ResponseHeaders);
        AppendSection(builder, "Response Body", diff.ResponseBody);
        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, string title, IReadOnlyList<LineDiffSegment> segments)
    {
        if (segments.Count == 0)
        {
            return;
        }

        var allEqual = true;
        for (var index = 0; index < segments.Count; index++)
        {
            if (segments[index].Operation != LineDiffOperation.Equal)
            {
                allEqual = false;
                break;
            }
        }

        if (allEqual)
        {
            return;
        }

        builder.Append("--- ").Append(title).Append(" (old)\n");
        builder.Append("+++ ").Append(title).Append(" (new)\n");
        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            var marker = GetMarker(segment.Operation);
            builder.Append(marker).Append(segment.Text).Append('\n');
        }
    }

    private static char GetMarker(LineDiffOperation operation)
    {
        if (operation == LineDiffOperation.Insert)
        {
            return '+';
        }

        if (operation == LineDiffOperation.Delete)
        {
            return '-';
        }

        return ' ';
    }
}
