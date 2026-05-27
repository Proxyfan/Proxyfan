namespace Proxyfan.Framework.Networking;

/// <summary>
///     Result of a state-machine transition attempt on an HTTP/2 stream. The next state and
///     a flag indicating whether the transition was rejected as a protocol error are exposed
///     separately so that callers can react to invalid transitions without exceptions.
/// </summary>
public readonly record struct HypertextTransferProtocolVersion2StreamTransitionResult
{
    /// <summary>
    ///     Gets a value indicating whether the transition was rejected as a protocol error.
    /// </summary>
    public bool IsProtocolError { get; }

    /// <summary>
    ///     Gets the next state for the stream after the transition was applied.
    /// </summary>
    public HypertextTransferProtocolVersion2StreamState NextState { get; }

    /// <summary>
    ///     Initializes a new transition result.
    /// </summary>
    /// <param name="nextState">The next state after the transition.</param>
    /// <param name="isProtocolError">When <c>true</c> the transition was rejected.</param>
    public HypertextTransferProtocolVersion2StreamTransitionResult(
        HypertextTransferProtocolVersion2StreamState nextState,
        bool isProtocolError)
    {
        NextState = nextState;
        IsProtocolError = isProtocolError;
    }
}
