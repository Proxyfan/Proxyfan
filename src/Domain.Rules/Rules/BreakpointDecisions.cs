using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Factory helpers for creating <see cref="BreakpointDecision" /> instances.
/// </summary>
public static class BreakpointDecisions
{
    /// <summary>
    ///     Creates a decision that aborts the in-flight request without forwarding it.
    /// </summary>
    /// <returns>An abort decision.</returns>
    public static BreakpointDecision Abort()
    {
        return new BreakpointDecision(isAborting: true, modifiedRequest: null, modifiedResponse: null);
    }

    /// <summary>
    ///     Creates a request-phase decision that resumes processing with a (possibly modified) request.
    /// </summary>
    /// <param name="request">The (possibly modified) request to forward.</param>
    /// <returns>A request-phase decision.</returns>
    public static BreakpointDecision ResumeRequest(HypertextTransferProtocolRequestData request)
    {
        return new BreakpointDecision(isAborting: false, modifiedRequest: request, modifiedResponse: null);
    }

    /// <summary>
    ///     Creates a response-phase decision that resumes processing with a (possibly modified) response.
    /// </summary>
    /// <param name="response">The (possibly modified) response to deliver.</param>
    /// <returns>A response-phase decision.</returns>
    public static BreakpointDecision ResumeResponse(HypertextTransferProtocolResponseData response)
    {
        return new BreakpointDecision(isAborting: false, modifiedRequest: null, modifiedResponse: response);
    }
}
