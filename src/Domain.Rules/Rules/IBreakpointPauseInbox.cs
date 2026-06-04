using System.Collections.Generic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Inbox of pending <see cref="BreakpointPause" /> instances awaiting user resolution.
///     The interactive breakpoint handler pushes pauses into the inbox; the UI consumes
///     and resolves them.
/// </summary>
public interface IBreakpointPauseInbox
{
    /// <summary>
    ///     Raised on the thread that called <see cref="Add" /> when a new pause is added.
    /// </summary>
    event BreakpointPauseInboxChanged? PauseAdded;

    /// <summary>
    ///     Raised on the thread that resolved the pause when it has been resolved (resumed or aborted).
    /// </summary>
    event BreakpointPauseInboxChanged? PauseResolved;

    /// <summary>
    ///     Gets the current number of pending pauses.
    /// </summary>
    int PendingCount { get; }

    /// <summary>
    ///     Aborts the supplied pause, removes it from the inbox, and raises <see cref="PauseResolved" />.
    /// </summary>
    /// <param name="pause">The pause being aborted.</param>
    void Abort(BreakpointPause pause);

    /// <summary>
    ///     Adds a new pause to the inbox and raises <see cref="PauseAdded" />.
    /// </summary>
    /// <param name="pause">The pause to enqueue.</param>
    void Add(BreakpointPause pause);

    /// <summary>
    ///     Returns a snapshot of the currently pending pauses.
    /// </summary>
    /// <returns>A snapshot list of pending pauses in arrival order.</returns>
    IReadOnlyList<BreakpointPause> GetPending();

    /// <summary>
    ///     Resolves the supplied pause by calling <see cref="BreakpointPause.ResumeWith" />
    ///     with the supplied <paramref name="decision" />, removes it from the inbox, and raises
    ///     <see cref="PauseResolved" />.
    /// </summary>
    /// <param name="pause">The pause being resolved.</param>
    /// <param name="decision">The decision to apply.</param>
    void Resolve(BreakpointPause pause, BreakpointDecision decision);
}
