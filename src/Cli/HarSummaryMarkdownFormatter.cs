using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Proxyfan.Cli;

/// <summary>
///     Formats a collection of <see cref="TrafficFlow" /> values as a GitHub-Flavored
///     Markdown table, suitable for pasting into issues or pull requests when sharing
///     captured HTTP traffic.
/// </summary>
public static class HarSummaryMarkdownFormatter
{
    /// <summary>
    ///     Builds the Markdown table.
    /// </summary>
    /// <param name="flows">The flows to render. May be empty.</param>
    /// <returns>The Markdown table, with header and separator rows.</returns>
    public static string Format(IReadOnlyList<TrafficFlow> flows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("| # | Status | Method | URL |");
        builder.AppendLine("|---|--------|--------|-----|");

        for (var index = 0; index < flows.Count; index++)
        {
            var flow = flows[index];
            AppendFlowRow(builder, index + 1, flow);
        }

        return builder.ToString();
    }

    private static void AppendFlowRow(StringBuilder builder, int sequenceNumber, TrafficFlow flow)
    {
        var method = flow.Request?.Method ?? "-";
        var url = flow.Request?.RequestUri.ToString() ?? "(no request)";
        var status = flow.Response?.StatusCode.ToString(CultureInfo.InvariantCulture) ?? "---";
        builder.Append("| ");
        builder.Append(sequenceNumber.ToString(CultureInfo.InvariantCulture));
        builder.Append(" | ");
        builder.Append(EscapeTableCell(status));
        builder.Append(" | ");
        builder.Append(EscapeTableCell(method));
        builder.Append(" | `");
        builder.Append(EscapeTableCell(EscapeBackticks(url)));
        builder.AppendLine("` |");
    }

    private static string EscapeBackticks(string value)
    {
        if (value.Contains('`', System.StringComparison.Ordinal))
        {
            return value.Replace("`", "\\`", System.StringComparison.Ordinal);
        }

        return value;
    }

    private static string EscapeTableCell(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        var hasPipe = value.Contains('|', System.StringComparison.Ordinal);
        var hasNewline = value.Contains('\n', System.StringComparison.Ordinal)
            || value.Contains('\r', System.StringComparison.Ordinal);
        if (!hasPipe && !hasNewline)
        {
            return value;
        }

        var normalized = value
            .Replace("\r\n", " ", System.StringComparison.Ordinal)
            .Replace("\r", " ", System.StringComparison.Ordinal)
            .Replace("\n", " ", System.StringComparison.Ordinal)
            .Replace("|", "\\|", System.StringComparison.Ordinal);
        return normalized;
    }
}
