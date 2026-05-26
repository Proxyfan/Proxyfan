using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents request and response timing milestones for a traffic flow.
/// </summary>
public sealed class FlowTimings
{
    /// <summary>
    ///     Gets the time at which the request completed.
    /// </summary>
    public DateTimeOffset? RequestCompletedAt { get; }

    /// <summary>
    ///     Gets the time at which the request started.
    /// </summary>
    public DateTimeOffset? RequestStartedAt { get; }

    /// <summary>
    ///     Gets the time at which the response completed.
    /// </summary>
    public DateTimeOffset? ResponseCompletedAt { get; }

    /// <summary>
    ///     Gets the time at which the response started.
    /// </summary>
    public DateTimeOffset? ResponseStartedAt { get; }

    /// <summary>
    ///     Gets the total flow duration when both the request start and response completion are known.
    /// </summary>
    public TimeSpan? TotalDuration
    {
        get
        {
            if (RequestStartedAt.HasValue && ResponseCompletedAt.HasValue)
            {
                return ResponseCompletedAt.Value - RequestStartedAt.Value;
            }

            return null;
        }
    }

    /// <summary>
    ///     Initializes a new <see cref="FlowTimings" /> instance.
    /// </summary>
    /// <param name="requestStartedAt">
    ///     The time at which the request started.
    /// </param>
    /// <param name="requestCompletedAt">
    ///     The time at which the request completed.
    /// </param>
    /// <param name="responseStartedAt">
    ///     The time at which the response started.
    /// </param>
    /// <param name="responseCompletedAt">
    ///     The time at which the response completed.
    /// </param>
    public FlowTimings(
        DateTimeOffset? requestStartedAt,
        DateTimeOffset? requestCompletedAt,
        DateTimeOffset? responseStartedAt,
        DateTimeOffset? responseCompletedAt)
    {
        RequestCompletedAt = requestCompletedAt;
        RequestStartedAt = requestStartedAt;
        ResponseCompletedAt = responseCompletedAt;
        ResponseStartedAt = responseStartedAt;
    }
}