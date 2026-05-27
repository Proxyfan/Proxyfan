using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Defines the action a user takes to resolve a breakpoint pause.
/// </summary>
public sealed class BreakpointDecision
{
    /// <summary>
    ///     Gets a value indicating whether the proxy should abort the request and close the connection.
    /// </summary>
    public bool IsAborting { get; }

    /// <summary>
    ///     Gets the (possibly modified) request data to forward. <see langword="null" /> when
    ///     this decision is for the response phase.
    /// </summary>
    public HypertextTransferProtocolRequestData? ModifiedRequest { get; }

    /// <summary>
    ///     Gets the (possibly modified) response data to deliver. <see langword="null" /> when
    ///     this decision is for the request phase.
    /// </summary>
    public HypertextTransferProtocolResponseData? ModifiedResponse { get; }

    /// <summary>
    ///     Initializes a new <see cref="BreakpointDecision" />.
    /// </summary>
    /// <param name="isAborting">Whether the request should be aborted.</param>
    /// <param name="modifiedRequest">The (possibly modified) request to forward.</param>
    /// <param name="modifiedResponse">The (possibly modified) response to deliver.</param>
    public BreakpointDecision(
        bool isAborting,
        HypertextTransferProtocolRequestData? modifiedRequest,
        HypertextTransferProtocolResponseData? modifiedResponse)
    {
        IsAborting = isAborting;
        ModifiedRequest = modifiedRequest;
        ModifiedResponse = modifiedResponse;
    }
}
