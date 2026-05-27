using Proxyfan.Domain.Traffic;
using System.Globalization;
using System.Text;

namespace Proxyfan.Cli;

/// <summary>
///     Formats <see cref="TrafficFlow" /> data for human-readable CLI output.
/// </summary>
public static class HarSummaryFormatter
{
    /// <summary>
    ///     Builds a single summary line for one traffic flow.
    /// </summary>
    /// <param name="sequenceNumber">The 1-based sequence number of the flow.</param>
    /// <param name="flow">The traffic flow.</param>
    /// <returns>A formatted summary line.</returns>
    public static string BuildFlowLine(int sequenceNumber, TrafficFlow flow)
    {
        var builder = new StringBuilder();
        builder.Append("  ").Append(sequenceNumber.ToString(CultureInfo.InvariantCulture)).Append(". ");
        var method = flow.Request?.Method ?? "-";
        var url = flow.Request?.RequestUri.ToString() ?? "(no request)";
        var status = flow.Response?.StatusCode.ToString(CultureInfo.InvariantCulture) ?? "---";
        builder.Append(status).Append(' ').Append(method).Append(' ').Append(url);
        return builder.ToString();
    }
}
