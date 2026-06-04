using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Proxyfan.Cli;

/// <summary>
///     Formats aggregated metrics over a HAR document into a human-readable text report.
///     Used by the <c>har-stats</c> CLI command to give CI / ops a quick at-a-glance summary
///     of a capture without scrolling through hundreds of individual flow lines.
/// </summary>
public static class HarStatsFormatter
{
    /// <summary>
    ///     Builds the machine-readable JSON report from the supplied flows.
    /// </summary>
    /// <param name="flows">The flows to summarise.</param>
    /// <returns>The JSON report.</returns>
    public static string BuildJsonReport(IReadOnlyList<TrafficFlow> flows)
    {
        var report = BuildStructuredReport(flows);
        return CliJsonWriter.Serialize(report) + "\n";
    }

    /// <summary>
    ///     Builds the multi-line text report from the supplied flows.
    /// </summary>
    /// <param name="flows">The flows to summarise.</param>
    /// <returns>The text report.</returns>
    public static string BuildReport(IReadOnlyList<TrafficFlow> flows)
    {
        var report = BuildStructuredReport(flows);
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Total flows: {report.TotalFlows}");

        if (report.TotalFlows == 0)
        {
            return builder.ToString();
        }

        AppendStatusDistribution(builder, report);
        AppendMethodDistribution(builder, report);
        AppendByteTotals(builder, report);
        AppendDurationStats(builder, report);
        return builder.ToString();
    }

    /// <summary>
    ///     Builds the structured report from the supplied flows.
    /// </summary>
    /// <param name="flows">The flows to summarise.</param>
    /// <returns>The structured report.</returns>
    public static HarStatsReport BuildStructuredReport(IReadOnlyList<TrafficFlow> flows)
    {
        long requestBytes = 0;
        long responseBytes = 0;
        var byMethod = new SortedDictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        var byStatusClass = new SortedDictionary<string, int>(System.StringComparer.Ordinal);
        var durations = new List<double>(flows.Count);

        foreach (var flow in flows)
        {
            requestBytes += flow.Request?.Body.Length ?? 0;
            responseBytes += flow.Response?.Body.Length ?? 0;
            AddCount(byMethod, flow.Request?.Method ?? "-");
            AddCount(byStatusClass, ClassifyStatus(flow));

            if (flow.Timings.RequestStartedAt.HasValue && flow.Timings.RequestCompletedAt.HasValue)
            {
                durations.Add((flow.Timings.RequestCompletedAt.Value - flow.Timings.RequestStartedAt.Value).TotalMilliseconds);
            }
        }

        durations.Sort();
        HarStatsDurationSummary? durationSummary = null;
        if (durations.Count > 0)
        {
            var medianIndex = durations.Count / 2;
            var report = new HarStatsDurationSummary
            {
                Max = durations[^1],
                Median = durations[medianIndex],
                Min = durations[0],
                Samples = durations.Count,
            };
            durationSummary = report;
        }

        return new HarStatsReport
        {
            DurationMilliseconds = durationSummary,
            Methods = byMethod,
            RequestBodyBytes = requestBytes,
            ResponseBodyBytes = responseBytes,
            StatusClasses = byStatusClass,
            TotalFlows = flows.Count,
        };
    }

    private static void AddCount(SortedDictionary<string, int> counts, string key)
    {
        if (counts.TryGetValue(key, out var existing))
        {
            counts[key] = existing + 1;
            return;
        }

        counts[key] = 1;
    }

    private static void AppendByteTotals(StringBuilder builder, HarStatsReport report)
    {
        builder.AppendLine(CultureInfo.InvariantCulture, $"Request body bytes:  {report.RequestBodyBytes:N0}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Response body bytes: {report.ResponseBodyBytes:N0}");
    }

    private static void AppendDurationStats(StringBuilder builder, HarStatsReport report)
    {
        if (report.DurationMilliseconds is null)
        {
            return;
        }

        builder.AppendLine(
            CultureInfo.InvariantCulture,
            $"Duration (ms): min={report.DurationMilliseconds.Min:F0} median={report.DurationMilliseconds.Median:F0} max={report.DurationMilliseconds.Max:F0} samples={report.DurationMilliseconds.Samples}");
    }

    private static void AppendMethodDistribution(StringBuilder builder, HarStatsReport report)
    {
        builder.AppendLine("Methods:");

        foreach (var entry in report.Methods)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"  {entry.Key,-8} {entry.Value}");
        }
    }

    private static void AppendStatusDistribution(StringBuilder builder, HarStatsReport report)
    {
        builder.AppendLine("Status classes:");

        foreach (var entry in report.StatusClasses)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"  {entry.Key,-5} {entry.Value}");
        }
    }

    private static string ClassifyStatus(TrafficFlow flow)
    {
        if (flow.Response is null)
        {
            return "(no response)";
        }

        var statusCode = flow.Response.StatusCode;

        if (statusCode is >= 200 and < 300)
        {
            return "2xx";
        }

        if (statusCode is >= 300 and < 400)
        {
            return "3xx";
        }

        if (statusCode is >= 400 and < 500)
        {
            return "4xx";
        }

        if (statusCode is >= 500 and < 600)
        {
            return "5xx";
        }

        return "other";
    }
}
