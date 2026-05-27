namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Delegate raised when an <see cref="IBreakpointPauseInbox" /> changes state.
/// </summary>
/// <param name="pause">The pause whose state changed.</param>
public delegate void BreakpointPauseInboxChanged(BreakpointPause pause);
