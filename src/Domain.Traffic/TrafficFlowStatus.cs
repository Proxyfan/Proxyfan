namespace Proxyfan.Domain.Traffic;

/// <summary>Represents the lifecycle status of a <see cref="TrafficFlow" />.</summary>
public enum TrafficFlowStatus
{
    /// <summary>The flow has been created but no data has been exchanged yet.</summary>
    Pending,

    /// <summary>The flow is actively transferring data.</summary>
    Active,

    /// <summary>The flow completed successfully.</summary>
    Completed,

    /// <summary>The flow terminated due to an error.</summary>
    Failed,
}
