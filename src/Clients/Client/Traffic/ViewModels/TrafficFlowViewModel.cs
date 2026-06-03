using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     View model for a single captured traffic flow row in the traffic list.
/// </summary>
public sealed partial class TrafficFlowViewModel : ObservableObject
{
    [ObservableProperty]
    private long _bodySize;
    [ObservableProperty]
    private TrafficFlowColorTag _colorTag;
    [ObservableProperty]
    private string? _comment;
    [ObservableProperty]
    private TimeSpan? _duration;
    [ObservableProperty]
    private TrafficFlowStatus _flowStatus;
    [ObservableProperty]
    private HypertextTransferProtocolResponseData? _response;
    [ObservableProperty]
    private int _statusCode;
    [ObservableProperty]
    private FlowTimings? _timings;

    /// <summary>
    ///     Gets the remote client endpoint address.
    /// </summary>
    public string ClientEndPoint { get; }

    /// <summary>
    ///     Gets the Graph Query Language (GraphQL) operation name extracted from the request,
    ///     or <see langword="null" /> when the request is not GraphQL.
    /// </summary>
    public string? GraphQueryLanguageOperationName { get; }

    /// <summary>
    ///     Gets the target host extracted from the request.
    /// </summary>
    public string Host { get; }

    /// <summary>
    ///     Gets the unique identifier of the flow.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Gets the request HTTP method.
    /// </summary>
    public string Method { get; }

    /// <summary>
    ///     Gets the sequential display number for this flow.
    /// </summary>
    public int Number { get; }

    /// <summary>
    ///     Gets the request path and query string.
    /// </summary>
    public string PathAndQuery { get; }

    /// <summary>
    ///     Gets the captured request data, when available.
    /// </summary>
    public HypertextTransferProtocolRequestData? Request { get; }

    /// <summary>
    ///     Gets the UTC instant at which the flow started.
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    ///     Initializes a new <see cref="TrafficFlowViewModel" /> from a live <see cref="RequestReceived" /> event.
    /// </summary>
    /// <param name="requestEvent">
    ///     The request-received domain event carrying request data.
    /// </param>
    /// <param name="number">
    ///     The sequential display number for this flow.
    /// </param>
    public TrafficFlowViewModel(RequestReceived requestEvent, int number)
    {
        ClientEndPoint = requestEvent.ClientEndPoint;
        GraphQueryLanguageOperationName = TrafficFlowGraphQueryLanguageOperationExtractor.Extract(requestEvent.Request);
        Host = requestEvent.Request.Headers.Get("Host") ?? "(tunnel)";
        Id = requestEvent.TrafficFlowId;
        Method = requestEvent.Request.Method;
        Number = number;
        PathAndQuery = requestEvent.Request.RequestUri.PathAndQuery;
        Request = requestEvent.Request;
        StartedAt = requestEvent.Timestamp;
        _bodySize = 0;
        _colorTag = TrafficFlowColorTag.None;
        _comment = null;
        _duration = null;
        _flowStatus = TrafficFlowStatus.Active;
        _response = null;
        _statusCode = 0;
        _timings = null;
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficFlowViewModel" /> from a persisted <see cref="TrafficFlow" />.
    /// </summary>
    /// <param name="flow">
    ///     The persisted traffic flow.
    /// </param>
    /// <param name="number">
    ///     The sequential display number for this flow.
    /// </param>
    public TrafficFlowViewModel(TrafficFlow flow, int number)
    {
        ClientEndPoint = flow.ClientEndPoint;
        GraphQueryLanguageOperationName = TrafficFlowGraphQueryLanguageOperationExtractor.Extract(flow.Request);
        Host = flow.Request?.Headers.Get("Host") ?? "(tunnel)";
        Id = flow.Id;
        Method = flow.Request?.Method ?? "CONNECT";
        Number = number;
        PathAndQuery = flow.Request?.RequestUri.PathAndQuery ?? "/";
        Request = flow.Request;
        StartedAt = flow.StartedAt;
        _bodySize = flow.Response?.Body.Length ?? 0;
        _colorTag = flow.ColorTag;
        _comment = flow.Comment;
        _duration = flow.Timings.TotalDuration;
        _flowStatus = flow.Status;
        _response = flow.Response;
        _statusCode = flow.Response?.StatusCode ?? 0;
        _timings = flow.Timings;
    }

    /// <summary>
    ///     Assigns the given color tag to this flow.
    /// </summary>
    /// <param name="colorTag">The color to assign; use <see cref="TrafficFlowColorTag.None" /> to clear.</param>
    public void ApplyColorTag(TrafficFlowColorTag colorTag)
    {
        ColorTag = colorTag;
    }

    /// <summary>
    ///     Assigns the given comment to this flow.
    /// </summary>
    /// <param name="comment">The comment text; <see langword="null" /> or whitespace clears it.</param>
    public void ApplyComment(string? comment)
    {
        Comment = comment;
    }

    /// <summary>
    ///     Replaces the timing milestones displayed by this view model with the supplied snapshot.
    /// </summary>
    /// <param name="timings">The updated timing milestones from the domain flow.</param>
    public void SetTimings(FlowTimings timings)
    {
        Timings = timings;
    }

    /// <summary>
    ///     Updates the view model with response data from a <see cref="ResponseReceived" /> event.
    /// </summary>
    /// <param name="responseEvent">
    ///     The response-received domain event carrying response data.
    /// </param>
    public void UpdateResponse(ResponseReceived responseEvent)
    {
        BodySize = responseEvent.Response.Body.Length;
        Response = responseEvent.Response;
        StatusCode = responseEvent.Response.StatusCode;
    }

    /// <summary>
    ///     Updates the terminal status and duration from a <see cref="TrafficFlowCompleted" /> event.
    /// </summary>
    /// <param name="completedEvent">
    ///     The flow-completed domain event carrying the terminal status.
    /// </param>
    public void UpdateStatus(TrafficFlowCompleted completedEvent)
    {
        Duration = completedEvent.Timestamp - StartedAt;
        FlowStatus = completedEvent.Status;
    }
}
