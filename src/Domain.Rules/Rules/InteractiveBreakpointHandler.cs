using Proxyfan.Domain.Traffic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Interactive <see cref="IBreakpointHandler" /> that pushes breakpoint pauses into an
///     <see cref="IBreakpointPauseInbox" /> and awaits resolution by the UI. When the
///     <see cref="MutableBreakpointConfiguration" /> indicates the request URL does not match
///     the active patterns or the phase is suppressed, the handler resumes immediately without
///     creating a pause.
/// </summary>
public sealed class InteractiveBreakpointHandler : IBreakpointHandler
{
    private readonly MutableBreakpointConfiguration _configuration;
    private readonly IBreakpointPauseInbox _inbox;

    /// <summary>
    ///     Initializes a new <see cref="InteractiveBreakpointHandler" />.
    /// </summary>
    /// <param name="configuration">The breakpoint configuration to consult.</param>
    /// <param name="inbox">The inbox that surfaces pending pauses to the UI.</param>
    public InteractiveBreakpointHandler(
        MutableBreakpointConfiguration configuration,
        IBreakpointPauseInbox inbox)
    {
        _configuration = configuration;
        _inbox = inbox;
    }

    /// <inheritdoc />
    public async Task<BreakpointDecision> ResolveRequestAsync(
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken)
    {
        if (!_configuration.HasRequestMatch(request.RequestUri.AbsoluteUri))
        {
            return BreakpointDecisions.ResumeRequest(request);
        }

        var pause = new BreakpointPause(Guid.NewGuid(), request);
        return await WaitForDecisionAsync(pause, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BreakpointDecision> ResolveResponseAsync(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        CancellationToken cancellationToken)
    {
        if (!_configuration.HasResponseMatch(request.RequestUri.AbsoluteUri))
        {
            return BreakpointDecisions.ResumeResponse(response);
        }

        var pause = new BreakpointPause(Guid.NewGuid(), request, response);
        return await WaitForDecisionAsync(pause, cancellationToken).ConfigureAwait(false);
    }

    private async Task<BreakpointDecision> WaitForDecisionAsync(
        BreakpointPause pause,
        CancellationToken cancellationToken)
    {
        _inbox.Add(pause);

        var resolved = false;
        try
        {
            using var registration = cancellationToken.Register(state =>
            {
                if (state is BreakpointPause captured)
                {
                    captured.Cancel(CancellationToken.None);
                }
            }, pause);

            var decision = await pause.WaitForDecisionAsync(CancellationToken.None).ConfigureAwait(false);
            resolved = true;
            return decision;
        }
        finally
        {
            if (!resolved)
            {
                _inbox.Abort(pause);
            }
        }
    }
}
