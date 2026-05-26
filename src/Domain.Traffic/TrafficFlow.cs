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
    ///     Gets the UTC instant at which this flow transitioned to <see cref="TrafficFlowStatus.Failed" />.
    /// </summary>
    public DateTimeOffset? FailedAt { get; private set; }

    /// <summary>
    ///     Gets the unique identifier of this flow.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Gets the captured HTTP request, when available.
    /// </summary>
    public HypertextTransferProtocolRequestData? Request { get; private set; }

    /// <summary>
    ///     Gets the captured HTTP response, when available.
    /// </summary>
    public HypertextTransferProtocolResponseData? Response { get; private set; }

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
    {
        ClientEndPoint = clientEndPoint;
        Id = id;
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
    ///     Captures the HTTP response for an active flow.
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
        var flowTimings = new FlowTimings(Timings.RequestStartedAt, timestamp, timestamp, Timings.ResponseCompletedAt);
        Timings = flowTimings;
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