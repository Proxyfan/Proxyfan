using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents a single proxy traffic flow from request capture through completion.
/// </summary>
public sealed class TrafficFlow
{
    /// <summary>
    ///     Gets the client endpoint associated with this flow.
    /// </summary>
    public string ClientEndPoint { get; }

    /// <summary>
    ///     Gets the user-assigned colour tag for this flow. The default,
    ///     <see cref="TrafficFlowColorTag.None" />, indicates no colour has been set.
    /// </summary>
    public TrafficFlowColorTag ColorTag { get; private set; }

    /// <summary>
    ///     Gets the user-supplied comment for this flow, or <c>null</c> when none has
    ///     been set. Comments survive HAR export and are full-text searchable.
    /// </summary>
    public string? Comment { get; private set; }

    /// <summary>
    ///     Gets the UTC instant at which this flow transitioned to <see cref="TrafficFlowStatus.Failed" />.
    /// </summary>
    public DateTimeOffset? FailedAt { get; private set; }

    /// <summary>
    ///     Gets the unique identifier of this flow.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Gets a value indicating whether the captured request body was truncated to satisfy
    ///     configured limits.
    /// </summary>
    public bool IsRequestBodyTruncated { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the captured response body was truncated to satisfy
    ///     configured limits.
    /// </summary>
    public bool IsResponseBodyTruncated { get; private set; }

    /// <summary>
    ///     Gets the origin of this flow, indicating whether it was captured live, repeated
    ///     from a previous flow, or composed manually.
    /// </summary>
    public TrafficFlowOrigin Origin { get; }

    /// <summary>
    ///     Gets the captured HTTP request, when available.
    /// </summary>
    public HypertextTransferProtocolRequestData? Request { get; private set; }

    /// <summary>
    ///     Gets the on-disk spill path for the request body when the capture was externalized
    ///     from memory; otherwise <see langword="null" />.
    /// </summary>
    public string? RequestBodySpillFilePath { get; private set; }

    /// <summary>
    ///     Gets the captured HTTP response, when available.
    /// </summary>
    public HypertextTransferProtocolResponseData? Response { get; private set; }

    /// <summary>
    ///     Gets the on-disk spill path for the response body when the capture was externalized
    ///     from memory; otherwise <see langword="null" />.
    /// </summary>
    public string? ResponseBodySpillFilePath { get; private set; }

    /// <summary>
    ///     Gets the UTC instant at which this flow was created.
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    ///     Gets the current lifecycle status of this flow.
    /// </summary>
    public TrafficFlowStatus Status { get; private set; }

    /// <summary>
    ///     Gets the timing milestones associated with this flow.
    /// </summary>
    public FlowTimings Timings { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="TrafficFlow" /> in the <see cref="TrafficFlowStatus.Pending" /> state.
    /// </summary>
    /// <param name="id">
    ///     The unique flow identifier.
    /// </param>
    /// <param name="clientEndPoint">
    ///     The client endpoint associated with this flow.
    /// </param>
    /// <param name="startedAt">
    ///     The UTC instant at which this flow was created.
    /// </param>
    public TrafficFlow(Guid id, string clientEndPoint, DateTimeOffset startedAt)
        : this(id, clientEndPoint, startedAt, TrafficFlowOrigin.Captured)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficFlow" /> in the <see cref="TrafficFlowStatus.Pending" /> state
    ///     with an explicit origin annotation.
    /// </summary>
    /// <param name="id">
    ///     The unique flow identifier.
    /// </param>
    /// <param name="clientEndPoint">
    ///     The client endpoint associated with this flow.
    /// </param>
    /// <param name="startedAt">
    ///     The UTC instant at which this flow was created.
    /// </param>
    /// <param name="origin">
    ///     The origin of this flow.
    /// </param>
    public TrafficFlow(Guid id, string clientEndPoint, DateTimeOffset startedAt, TrafficFlowOrigin origin)
    {
        ClientEndPoint = clientEndPoint;
        Id = id;
        Origin = origin;
        StartedAt = startedAt;
        Status = TrafficFlowStatus.Pending;

        var flowTimings = new FlowTimings(null, null, null, null);
        Timings = flowTimings;
    }

    /// <summary>
    ///     Aborts the flow when it has not already reached a terminal state.
    /// </summary>
    public void Abort()
    {
        if (HasReachedTerminalStatus())
        {
            return;
        }

        Status = TrafficFlowStatus.Aborted;
    }

    /// <summary>
    ///     Completes the flow when it is active.
    /// </summary>
    public void Complete()
    {
        if (Status == TrafficFlowStatus.Complete)
        {
            return;
        }

        if (Status != TrafficFlowStatus.Active)
        {
            throw new InvalidOperationException("Only active flows can be completed.");
        }

        Status = TrafficFlowStatus.Complete;

        var timestamp = DateTimeOffset.UtcNow;
        var flowTimings = new FlowTimings(
            Timings.RequestStartedAt,
            Timings.RequestCompletedAt,
            Timings.ResponseStartedAt,
            timestamp);
        Timings = flowTimings;
    }

    /// <summary>
    ///     Fails the flow when it has not already reached a terminal state.
    /// </summary>
    public void Fail()
    {
        if (Status == TrafficFlowStatus.Failed)
        {
            return;
        }

        if (HasReachedTerminalStatus())
        {
            return;
        }

        Status = TrafficFlowStatus.Failed;
        FailedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Records the instant at which the request body finished being sent to the
    ///     upstream origin. No-op when the flow is not active or when a
    ///     request-completed timestamp has already been captured (idempotent).
    /// </summary>
    public void MarkRequestCompleted()
    {
        if (Status != TrafficFlowStatus.Active)
        {
            return;
        }

        if (Timings.RequestCompletedAt.HasValue)
        {
            return;
        }

        var timestamp = DateTimeOffset.UtcNow;
        var flowTimings = new FlowTimings(
            Timings.RequestStartedAt,
            timestamp,
            Timings.ResponseStartedAt,
            Timings.ResponseCompletedAt);
        Timings = flowTimings;
    }

    /// <summary>
    ///     Records the instant at which the first byte of the upstream response was
    ///     observed. No-op when the flow is not active or when a response-started
    ///     timestamp has already been captured (idempotent).
    /// </summary>
    public void MarkResponseStarted()
    {
        if (Status != TrafficFlowStatus.Active)
        {
            return;
        }

        if (Timings.ResponseStartedAt.HasValue)
        {
            return;
        }

        var timestamp = DateTimeOffset.UtcNow;
        var flowTimings = new FlowTimings(
            Timings.RequestStartedAt,
            Timings.RequestCompletedAt,
            timestamp,
            Timings.ResponseCompletedAt);
        Timings = flowTimings;
    }

    /// <summary>
    ///     Sets the user-assigned colour tag for this flow.
    /// </summary>
    /// <param name="colorTag">
    ///     The colour to assign. Use <see cref="TrafficFlowColorTag.None" /> to clear.
    /// </param>
    public void SetColorTag(TrafficFlowColorTag colorTag)
    {
        ColorTag = colorTag;
    }

    /// <summary>
    ///     Sets the user-supplied comment for this flow. Passing <c>null</c> or whitespace
    ///     clears the comment.
    /// </summary>
    /// <param name="comment">
    ///     The comment text, or <c>null</c> to clear.
    /// </param>
    public void SetComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            Comment = null;
            return;
        }

        Comment = comment;
    }

    /// <summary>
    ///     Captures the HTTP request and transitions the flow to <see cref="TrafficFlowStatus.Active" />.
    /// </summary>
    /// <param name="request">
    ///     The captured HTTP request.
    /// </param>
    public void SetRequest(HypertextTransferProtocolRequestData request)
    {
        if (Status != TrafficFlowStatus.Pending)
        {
            throw new InvalidOperationException("Only pending flows can accept a request.");
        }

        Request = request;
        Status = TrafficFlowStatus.Active;

        var timestamp = DateTimeOffset.UtcNow;
        var flowTimings = new FlowTimings(timestamp, Timings.RequestCompletedAt, Timings.ResponseStartedAt, Timings.ResponseCompletedAt);
        Timings = flowTimings;
    }

    /// <summary>
    ///     Captures the HTTP response for an active flow. When request-completed and
    ///     response-started milestones have not been recorded via
    ///     <see cref="MarkRequestCompleted" />/<see cref="MarkResponseStarted" /> they
    ///     fall back to the current instant; previously captured milestones are
    ///     preserved so the waiting/TTFB phase can be measured accurately.
    /// </summary>
    /// <param name="response">
    ///     The captured HTTP response.
    /// </param>
    public void SetResponse(HypertextTransferProtocolResponseData response)
    {
        if (Status != TrafficFlowStatus.Active)
        {
            throw new InvalidOperationException("Only active flows can accept a response.");
        }

        Response = response;

        var timestamp = DateTimeOffset.UtcNow;
        var requestCompletedAt = Timings.RequestCompletedAt ?? timestamp;
        var responseStartedAt = Timings.ResponseStartedAt ?? timestamp;
        var flowTimings = new FlowTimings(Timings.RequestStartedAt, requestCompletedAt, responseStartedAt, Timings.ResponseCompletedAt);
        Timings = flowTimings;
    }

    /// <summary>
    ///     Rewrites only the stored request body representation for retention, preserving
    ///     request metadata and flow lifecycle state.
    /// </summary>
    /// <param name="body">The replacement stored request body bytes.</param>
    /// <param name="spillFilePath">The spill path when body bytes are externalized.</param>
    /// <param name="isTruncated">Whether the original body exceeded retention limits.</param>
    public void UpdateRequestBodyForStorage(ReadOnlyMemory<byte> body, string? spillFilePath, bool isTruncated)
    {
        if (Request is null)
        {
            return;
        }

        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = body,
            Headers = Request.Headers,
            Method = Request.Method,
            RequestUri = Request.RequestUri,
            Version = Request.Version,
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        Request = request;
        RequestBodySpillFilePath = spillFilePath;
        IsRequestBodyTruncated = isTruncated;
    }

    /// <summary>
    ///     Rewrites only the stored response body representation for retention, preserving
    ///     response metadata and flow lifecycle state.
    /// </summary>
    /// <param name="body">The replacement stored response body bytes.</param>
    /// <param name="spillFilePath">The spill path when body bytes are externalized.</param>
    /// <param name="isTruncated">Whether the original body exceeded retention limits.</param>
    public void UpdateResponseBodyForStorage(ReadOnlyMemory<byte> body, string? spillFilePath, bool isTruncated)
    {
        if (Response is null)
        {
            return;
        }

        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = Response.Headers,
            ReasonPhrase = Response.ReasonPhrase,
            StatusCode = Response.StatusCode,
            Version = Response.Version,
        };
        var response = new HypertextTransferProtocolResponseData(parameters);
        Response = response;
        ResponseBodySpillFilePath = spillFilePath;
        IsResponseBodyTruncated = isTruncated;
    }

    private bool HasReachedTerminalStatus()
    {
        if (Status is TrafficFlowStatus.Complete or TrafficFlowStatus.Failed or TrafficFlowStatus.Aborted)
        {
            return true;
        }

        return false;
    }
}