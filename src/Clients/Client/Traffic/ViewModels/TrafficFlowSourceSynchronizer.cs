using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Propagates terminal-status transitions from a flow-completed domain event onto
///     the underlying domain <see cref="TrafficFlow" />. Kept in a dedicated static class
///     because the domain mutation must not live inside the view-model (MVVM boundary) and
///     analyzer rule ATXCS011 forbids static methods in non-static classes.
/// </summary>
public static class TrafficFlowSourceSynchronizer
{
    /// <summary>
    ///     Applies the terminal <paramref name="status" /> to <paramref name="source" />.
    ///     The <see cref="TrafficFlowStatus.Complete" /> path is guarded by an Active check
    ///     because <see cref="TrafficFlow.Complete" /> throws for flows that are not active.
    ///     <see cref="TrafficFlow.Fail" /> and <see cref="TrafficFlow.Abort" /> are
    ///     self-guarding: they silently no-op when the flow has already reached any terminal
    ///     state, so no caller-side guard is required for those paths.
    /// </summary>
    /// <param name="source">The domain flow to transition.</param>
    /// <param name="status">The target terminal status.</param>
    public static void SynchronizeStatus(TrafficFlow source, TrafficFlowStatus status)
    {
        if (status == TrafficFlowStatus.Complete && source.Status == TrafficFlowStatus.Active)
        {
            source.Complete();
            return;
        }

        if (status == TrafficFlowStatus.Failed)
        {
            source.Fail();
            return;
        }

        if (status == TrafficFlowStatus.Aborted)
        {
            source.Abort();
        }
    }
}
