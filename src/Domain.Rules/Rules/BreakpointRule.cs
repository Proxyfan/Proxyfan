using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     First-class <see cref="IAsyncRequestPhaseRule" /> and <see cref="IAsyncResponsePhaseRule" />
///     adapter that integrates <see cref="IBreakpointHandler" /> into the rule engine.
///     <para>
///         For the <b>request phase</b> the rule runs at priority 10 000 — after all synchronous
///         rules — so that redirect and modify actions from earlier rules are already applied to the
///         effective request that is presented to the user. A <see cref="RequestPipelineAction.Pause" />
///         is returned when the user aborts; <see cref="RequestPipelineAction.ModifyRequest" /> when
///         the user accepts a (possibly modified) request; <see langword="null" /> when the URL does
///         not match or the breakpoint is disabled.
///     </para>
///     <para>
///         For the <b>response phase</b> the rule runs at priority 20 000 — after scripting — so the
///         user sees the script-projected response. A <see cref="ResponsePipelineAction.Pause" /> is
///         returned on abort; <see cref="ResponsePipelineAction.ModifyResponse" /> on accept with
///         changes; <see langword="null" /> otherwise.
///     </para>
/// </summary>
public sealed class BreakpointRule : IAsyncRequestPhaseRule, IAsyncResponsePhaseRule
{
    private const int RequestPhasePriority = 10_000;
    private const int ResponsePhasePriority = 20_000;
    private readonly IBreakpointHandler _handler;

    /// <summary>
    ///     Initializes a new <see cref="BreakpointRule" />.
    /// </summary>
    /// <param name="handler">The breakpoint handler that presents pauses to the user interface.</param>
    public BreakpointRule(IBreakpointHandler handler)
    {
        _handler = handler;
    }

    int IAsyncRequestPhaseRule.Priority => RequestPhasePriority;

    int IAsyncResponsePhaseRule.Priority => ResponsePhasePriority;

    /// <inheritdoc />
    public async Task<RequestPipelineAction?> EvaluateRequestAsync(
        HypertextTransferProtocolRequestData request,
        string flowId,
        CancellationToken cancellationToken)
    {
        var decision = await _handler.ResolveRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (decision.IsAborting)
        {
            return new RequestPipelineAction.Pause();
        }

        var effectiveRequest = decision.ModifiedRequest ?? request;
        if (ReferenceEquals(effectiveRequest, request))
        {
            return null;
        }

        return new RequestPipelineAction.ModifyRequest(effectiveRequest);
    }

    /// <inheritdoc />
    public async Task<ResponsePipelineAction?> EvaluateResponseAsync(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        string flowId,
        CancellationToken cancellationToken)
    {
        var decision = await _handler.ResolveResponseAsync(request, response, cancellationToken).ConfigureAwait(false);

        if (decision.IsAborting)
        {
            return new ResponsePipelineAction.Pause();
        }

        var effectiveResponse = decision.ModifiedResponse ?? response;
        if (ReferenceEquals(effectiveResponse, response))
        {
            return null;
        }

        return new ResponsePipelineAction.ModifyResponse(effectiveResponse);
    }

    /// <inheritdoc cref="IAsyncRequestPhaseRule.IsEnabled" />
    public bool IsEnabled => true;
}
