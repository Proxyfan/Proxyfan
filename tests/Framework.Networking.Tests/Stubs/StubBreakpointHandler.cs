using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     Test stub for <see cref="IBreakpointHandler" /> that records invocations and returns
///     pre-configured decisions.
/// </summary>
public sealed class StubBreakpointHandler : IBreakpointHandler
{
    /// <summary>
    ///     Gets the count of request-phase resolutions.
    /// </summary>
    public int RequestResolveCount { get; private set; }

    /// <summary>
    ///     Gets the count of response-phase resolutions.
    /// </summary>
    public int ResponseResolveCount { get; private set; }

    /// <summary>
    ///     Gets or sets the decision to return from the next request-phase resolution.
    /// </summary>
    public BreakpointDecision? RequestDecision { get; set; }

    /// <summary>
    ///     Gets or sets the decision to return from the next response-phase resolution.
    /// </summary>
    public BreakpointDecision? ResponseDecision { get; set; }

    /// <inheritdoc />
    public Task<BreakpointDecision> ResolveRequestAsync(
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken)
    {
        RequestResolveCount++;
        var decision = RequestDecision ?? BreakpointDecisions.ResumeRequest(request);
        return Task.FromResult(decision);
    }

    /// <inheritdoc />
    public Task<BreakpointDecision> ResolveResponseAsync(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        CancellationToken cancellationToken)
    {
        ResponseResolveCount++;
        var decision = ResponseDecision ?? BreakpointDecisions.ResumeResponse(response);
        return Task.FromResult(decision);
    }
}
