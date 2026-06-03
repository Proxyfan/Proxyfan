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
    ///     No-ops that do not match an expected transition (e.g. double-completion) are
    ///     silently ignored because the domain object guards its own invariants.
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
