using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Default <see cref="IBreakpointPauseInbox" /> backed by a list guarded by a lock.
/// </summary>
public sealed class BreakpointPauseInbox : IBreakpointPauseInbox
{
    /// <inheritdoc />
    public event BreakpointPauseInboxChanged? PauseAdded;

    /// <inheritdoc />
    public event BreakpointPauseInboxChanged? PauseResolved;

    private readonly Lock _mutationLock;
    private readonly List<BreakpointPause> _pending;

    /// <summary>
    ///     Initializes a new empty <see cref="BreakpointPauseInbox" />.
    /// </summary>
    public BreakpointPauseInbox()
    {
        _pending = [];
        var mutationLock = new Lock();
        _mutationLock = mutationLock;
    }

    /// <inheritdoc />
    public void Abort(BreakpointPause pause)
    {
        var removed = HasRemovedPending(pause);
        if (!removed)
        {
            return;
        }

        pause.Abort();
        PauseResolved?.Invoke(pause);
    }

    /// <inheritdoc />
    public void Add(BreakpointPause pause)
    {
        lock (_mutationLock)
        {
            _pending.Add(pause);
        }

        PauseAdded?.Invoke(pause);
    }

    /// <inheritdoc />
    public IReadOnlyList<BreakpointPause> GetPending()
    {
        lock (_mutationLock)
        {
            return [.. _pending];
        }
    }

    /// <inheritdoc />
    public void Resolve(BreakpointPause pause, BreakpointDecision decision)
    {
        var removed = HasRemovedPending(pause);
        if (!removed)
        {
            return;
        }

        pause.ResumeWith(decision);
        PauseResolved?.Invoke(pause);
    }

    private bool HasRemovedPending(BreakpointPause pause)
    {
        lock (_mutationLock)
        {
            return _pending.Remove(pause);
        }
    }
}
