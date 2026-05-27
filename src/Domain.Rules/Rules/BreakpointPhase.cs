namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Identifies which phase(s) of a flow should trigger a breakpoint pause.
/// </summary>
[System.Flags]
public enum BreakpointPhase
{
    /// <summary>
    ///     No phase triggers a pause (rule disabled).
    /// </summary>
    None = 0,

    /// <summary>
    ///     Pause before forwarding the request to the upstream server so the user can edit it.
    /// </summary>
    Request = 1,

    /// <summary>
    ///     Pause after receiving the response and before delivering it to the client.
    /// </summary>
    Response = 2,

    /// <summary>
    ///     Pause on both phases.
    /// </summary>
    Both = Request | Response,
}
