namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Indicates how a traffic flow was created. Surfaces in the UI as a small annotation
///     so users can distinguish captured proxy traffic from manually composed or repeated
///     requests, mirroring Charles' "Repeat" tag and Fiddler's Composer history.
/// </summary>
public enum TrafficFlowOrigin
{
    /// <summary>
    ///     The flow was captured by the proxy listener (default).
    /// </summary>
    Captured = 0,

    /// <summary>
    ///     The flow was produced by repeating a previously captured request (Repeat Request).
    /// </summary>
    Repeated = 1,

    /// <summary>
    ///     The flow was produced by the Request Composer tool.
    /// </summary>
    Composed = 2,
}
