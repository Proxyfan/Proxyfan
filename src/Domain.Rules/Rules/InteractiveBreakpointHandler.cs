using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly ILogger<InteractiveBreakpointHandler> _logger;

    /// <summary>
    ///     Initializes a new <see cref="InteractiveBreakpointHandler" />.
    /// </summary>
    /// <param name="configuration">The breakpoint configuration to consult.</param>
    /// <param name="inbox">The inbox that surfaces pending pauses to the UI.</param>
    public InteractiveBreakpointHandler(
        MutableBreakpointConfiguration configuration,
        IBreakpointPauseInbox inbox)
        : this(configuration, inbox, NullLogger<InteractiveBreakpointHandler>.Instance)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="InteractiveBreakpointHandler" />.
    /// </summary>
    /// <param name="configuration">The breakpoint configuration to consult.</param>
    /// <param name="inbox">The inbox that surfaces pending pauses to the UI.</param>
    /// <param name="logger">Logger used to emit timeout auto-resume warnings.</param>
    public InteractiveBreakpointHandler(
        MutableBreakpointConfiguration configuration,
        IBreakpointPauseInbox inbox,
        ILogger<InteractiveBreakpointHandler> logger)
    {
        _configuration = configuration;
        _inbox = inbox;
        _logger = logger;
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

    private BreakpointDecision BuildResumeDecision(BreakpointPause pause)
    {
        if (pause.Phase == BreakpointPhase.Request)
        {
            return BreakpointDecisions.ResumeRequest(pause.Request);
        }

        var response = pause.Response ?? throw new InvalidOperationException("Response-phase pause must carry a response.");
        return BreakpointDecisions.ResumeResponse(response);
    }

    private void EnforcePauseLimitByResumingOldest()
    {
        if (_configuration.IsBackPressureEnabled)
        {
            return;
        }

        var maxPendingPauses = _configuration.MaxPendingPauses;
        while (_inbox.PendingCount >= maxPendingPauses)
        {
            var pending = _inbox.GetPending();
            if (pending.Count == 0)
            {
                return;
            }

            var oldest = pending[0];
            var before = _inbox.PendingCount;
            _inbox.Resolve(oldest, BuildResumeDecision(oldest));
            if (_inbox.PendingCount >= before)
            {
                return;
            }
        }
    }

    private bool HasBypassedBreakpointDueToBackPressure()
    {
        return _configuration.IsBackPressureEnabled
               && _inbox.PendingCount >= _configuration.MaxPendingPauses;
    }

    private async Task<BreakpointDecision> WaitForDecisionAsync(
        BreakpointPause pause,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (HasBypassedBreakpointDueToBackPressure())
        {
            return BuildResumeDecision(pause);
        }

        EnforcePauseLimitByResumingOldest();
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

            var decisionTask = pause.WaitForDecisionAsync(cancellationToken);
            var timeoutTask = Task.Delay(_configuration.PauseTimeout, CancellationToken.None);
            var winner = await Task.WhenAny(decisionTask, timeoutTask).ConfigureAwait(false);
            if (ReferenceEquals(winner, timeoutTask))
            {
                _logger.LogWarning(
                    "Breakpoint pause {PauseId} timed out after {PauseTimeout}. Auto-resuming without modifications.",
                    pause.PauseId,
                    _configuration.PauseTimeout);
                _inbox.Resolve(pause, BuildResumeDecision(pause));
            }

            var decision = await decisionTask.ConfigureAwait(false);
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
