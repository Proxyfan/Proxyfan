using Proxyfan.Domain.Traffic;
using System;
using System.Globalization;
using System.Text;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Immutable projection of a traffic flow for read-only UI consumption.
/// </summary>
public sealed class TrafficFlowSnapshot
{
    /// <summary>
    ///     Gets the client endpoint label.
    /// </summary>
    public string ClientEndPoint { get; }

    /// <summary>
    ///     Gets the projected color tag.
    /// </summary>
    public TrafficFlowColorTag ColorTag { get; }

    /// <summary>
    ///     Gets the projected comment.
    /// </summary>
    public string? Comment { get; }

    /// <summary>
    ///     Gets the projected flow identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Gets the projected request, when available.
    /// </summary>
    public HypertextTransferProtocolRequestData? Request { get; }

    /// <summary>
    ///     Gets the projected response, when available.
    /// </summary>
    public HypertextTransferProtocolResponseData? Response { get; }

    /// <summary>
    ///     Gets the projected lifecycle status.
    /// </summary>
    public TrafficFlowStatus Status { get; }

    /// <summary>
    ///     Gets the projected timing milestones.
    /// </summary>
    public FlowTimings Timings { get; }

    /// <summary>
    ///     Initializes a new <see cref="TrafficFlowSnapshot" /> instance.
    /// </summary>
    /// <param name="id">The flow identifier.</param>
    /// <param name="status">The projected lifecycle status.</param>
    /// <param name="clientEndPoint">The client endpoint label.</param>
    /// <param name="colorTag">The projected color tag.</param>
    /// <param name="comment">The projected comment.</param>
    /// <param name="request">The projected request, when available.</param>
    /// <param name="response">The projected response, when available.</param>
    /// <param name="timings">The projected timing milestones.</param>
    public TrafficFlowSnapshot(
        Guid id,
        TrafficFlowStatus status,
        string clientEndPoint,
        TrafficFlowColorTag colorTag,
        string? comment,
        HypertextTransferProtocolRequestData? request,
        HypertextTransferProtocolResponseData? response,
        FlowTimings timings)
    {
        Id = id;
        Status = status;
        ClientEndPoint = clientEndPoint;
        ColorTag = colorTag;
        Comment = comment;
        Request = request;
        Response = response;
        Timings = timings;
    }

    /// <summary>
    ///     Formats this snapshot as a multi-line key/value summary suitable for the inspector.
    /// </summary>
    /// <returns>The formatted summary text.</returns>
    public string FormatSummary()
    {
        var builder = new StringBuilder();
        AppendFlowMetadata(builder);
        AppendAnnotations(builder);
        AppendRequestSection(builder);
        AppendResponseSection(builder);
        AppendTotalDuration(builder);
        return builder.ToString();
    }

    private void AppendAnnotations(StringBuilder builder)
    {
        if (ColorTag != TrafficFlowColorTag.None)
        {
            builder.Append("Color tag: ");
            builder.AppendLine(ColorTag.ToString());
        }

        if (!string.IsNullOrEmpty(Comment))
        {
            builder.Append("Comment: ");
            builder.AppendLine(Comment);
        }
    }

    private void AppendFlowMetadata(StringBuilder builder)
    {
        builder.Append("Flow Id: ");
        builder.AppendLine(Id.ToString());
        builder.Append("Status: ");
        builder.AppendLine(Status.ToString());
        builder.Append("Client: ");
        builder.AppendLine(ClientEndPoint);
    }

    private void AppendRequestSection(StringBuilder builder)
    {
        if (Request is null)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Request");
        builder.Append("  Method: ");
        builder.AppendLine(Request.Method);
        builder.Append("  URI:    ");
        builder.AppendLine(Request.RequestUri.ToString());
        builder.Append("  Version: ");
        builder.AppendLine(Request.Version);
        var requestContentType = Request.Headers.Get("Content-Type");
        if (requestContentType is not null)
        {
            builder.Append("  Content-Type: ");
            builder.AppendLine(requestContentType);
        }

        builder.Append("  Body bytes: ");
        builder.AppendLine(Request.Body.Length.ToString(CultureInfo.InvariantCulture));
    }

    private void AppendResponseSection(StringBuilder builder)
    {
        if (Response is null)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Response");
        builder.Append("  Status: ");
        builder.Append(Response.StatusCode.ToString(CultureInfo.InvariantCulture));
        builder.Append(' ');
        builder.AppendLine(Response.ReasonPhrase);
        builder.Append("  Version: ");
        builder.AppendLine(Response.Version);
        var responseContentType = Response.Headers.Get("Content-Type");
        if (responseContentType is not null)
        {
            builder.Append("  Content-Type: ");
            builder.AppendLine(responseContentType);
        }

        var contentEncoding = Response.Headers.Get("Content-Encoding");
        if (contentEncoding is not null)
        {
            builder.Append("  Content-Encoding: ");
            builder.AppendLine(contentEncoding);
        }

        builder.Append("  Body bytes: ");
        builder.AppendLine(Response.Body.Length.ToString(CultureInfo.InvariantCulture));
    }

    private void AppendTotalDuration(StringBuilder builder)
    {
        if (!Timings.TotalDuration.HasValue)
        {
            return;
        }

        builder.AppendLine();
        builder.Append("Total duration: ");
        builder.Append(Timings.TotalDuration.Value.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture));
        builder.AppendLine(" ms");
    }
}
