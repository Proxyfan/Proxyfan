using System.Globalization;
using System.Text;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Renders a captured <see cref="TrafficFlow" /> as a multi-line key/value summary
///     suitable for the Summary inspector tab. Includes request line, response status,
///     content types, body sizes, client endpoint, and total duration.
/// </summary>
public static class FlowSummaryFormatter
{
    /// <summary>
    ///     Formats the supplied flow. Returns an empty string when the flow has neither
    ///     request nor response captured.
    /// </summary>
    /// <param name="flow">The flow to summarize.</param>
    /// <returns>The formatted summary text.</returns>
    public static string Format(TrafficFlow flow)
    {
        if (flow is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendFlowMetadata(builder, flow);
        AppendRequestSection(builder, flow.Request);
        AppendResponseSection(builder, flow.Response);
        AppendTotalDuration(builder, flow.Timings);
        return builder.ToString();
    }

    private static void AppendFlowMetadata(StringBuilder builder, TrafficFlow flow)
    {
        builder.Append("Flow Id: ");
        builder.AppendLine(flow.Id.ToString());
        builder.Append("Status: ");
        builder.AppendLine(flow.Status.ToString());
        builder.Append("Client: ");
        builder.AppendLine(flow.ClientEndPoint);
    }

    private static void AppendRequestSection(StringBuilder builder, HypertextTransferProtocolRequestData? request)
    {
        if (request is null)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Request");
        builder.Append("  Method: ");
        builder.AppendLine(request.Method);
        builder.Append("  URI:    ");
        builder.AppendLine(request.RequestUri.ToString());
        builder.Append("  Version: ");
        builder.AppendLine(request.Version);

        var requestContentType = request.Headers.Get("Content-Type");

        if (requestContentType is not null)
        {
            builder.Append("  Content-Type: ");
            builder.AppendLine(requestContentType);
        }

        builder.Append("  Body bytes: ");
        builder.AppendLine(request.Body.Length.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendResponseSection(StringBuilder builder, HypertextTransferProtocolResponseData? response)
    {
        if (response is null)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Response");
        builder.Append("  Status: ");
        builder.Append(response.StatusCode.ToString(CultureInfo.InvariantCulture));
        builder.Append(' ');
        builder.AppendLine(response.ReasonPhrase);
        builder.Append("  Version: ");
        builder.AppendLine(response.Version);

        var responseContentType = response.Headers.Get("Content-Type");

        if (responseContentType is not null)
        {
            builder.Append("  Content-Type: ");
            builder.AppendLine(responseContentType);
        }

        var contentEncoding = response.Headers.Get("Content-Encoding");

        if (contentEncoding is not null)
        {
            builder.Append("  Content-Encoding: ");
            builder.AppendLine(contentEncoding);
        }

        builder.Append("  Body bytes: ");
        builder.AppendLine(response.Body.Length.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendTotalDuration(StringBuilder builder, FlowTimings timings)
    {
        if (!timings.TotalDuration.HasValue)
        {
            return;
        }

        builder.AppendLine();
        builder.Append("Total duration: ");
        builder.Append(timings.TotalDuration.Value.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture));
        builder.AppendLine(" ms");
    }
}
