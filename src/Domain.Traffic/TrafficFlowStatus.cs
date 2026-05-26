namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents the lifecycle status of a <see cref="TrafficFlow" />.
/// </summary>
public enum TrafficFlowStatus
{
    /// <summary>
    ///     The flow has been created but no data has been exchanged yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    ///     The flow is actively transferring data.
    /// </summary>
    Active = 1,

    /// <summary>
    ///     The flow completed successfully.
    /// </summary>
    Complete = 2,

    /// <summary>
    ///     The flow terminated due to an error.
    /// </summary>
    Failed = 3,

    /// <summary>
    ///     The flow was aborted before completion.
    /// </summary>
    Aborted = 4,
}