using Proxyfan.Domain.Traffic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Represents a single in-flight breakpoint pause awaiting a user decision.
///     Created by <see cref="InteractiveBreakpointHandler" /> when a request or response
///     matches the configured breakpoint patterns; the breakpoint UI consumes the pause
///     and resolves it by calling <see cref="ResumeWith" /> or <see cref="Abort" />.
/// </summary>
public sealed class BreakpointPause
{
    private readonly TaskCompletionSource<BreakpointDecision> _completionSource;

    /// <summary>
    ///     Gets a value indicating whether the pause has already been resolved.
    /// </summary>
    public bool IsResolved => _completionSource.Task.IsCompleted;

    /// <summary>
    ///     Gets the unique identifier of the pause.
    /// </summary>
    public Guid PauseId { get; }

    /// <summary>
    ///     Gets the breakpoint phase represented by this pause.
    /// </summary>
    public BreakpointPhase Phase { get; }

    /// <summary>
    ///     Gets the captured request snapshot at the time the pause was opened.
    /// </summary>
    public HypertextTransferProtocolRequestData Request { get; }

    /// <summary>
    ///     Gets the captured response snapshot. <see langword="null" /> for request-phase pauses.
    /// </summary>
    public HypertextTransferProtocolResponseData? Response { get; }

    /// <summary>
    ///     Initializes a new <see cref="BreakpointPause" /> for the request phase.
    /// </summary>
    /// <param name="pauseId">The unique pause identifier.</param>
    /// <param name="request">The captured request snapshot.</param>
    public BreakpointPause(Guid pauseId, HypertextTransferProtocolRequestData request)
    {
        PauseId = pauseId;
        Phase = BreakpointPhase.Request;
        Request = request;
        Response = null;
        var source = new TaskCompletionSource<BreakpointDecision>(TaskCreationOptions.RunContinuationsAsynchronously);
        _completionSource = source;
    }

    /// <summary>
    ///     Initializes a new <see cref="BreakpointPause" /> for the response phase.
    /// </summary>
    /// <param name="pauseId">The unique pause identifier.</param>
    /// <param name="request">The captured request snapshot.</param>
    /// <param name="response">The captured response snapshot.</param>
    public BreakpointPause(
        Guid pauseId,
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response)
    {
        PauseId = pauseId;
        Phase = BreakpointPhase.Response;
        Request = request;
        Response = response;
        var source = new TaskCompletionSource<BreakpointDecision>(TaskCreationOptions.RunContinuationsAsynchronously);
        _completionSource = source;
    }

    /// <summary>
    ///     Aborts the in-flight pause, signalling the proxy pipeline to drop the request.
    /// </summary>
    public void Abort()
    {
        var decision = BreakpointDecisions.Abort();
        _completionSource.TrySetResult(decision);
    }

    /// <summary>
    ///     Cancels the in-flight pause, signalling the proxy pipeline that the caller
    ///     gave up waiting. Used when the proxy itself is being shut down.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to surface.</param>
    public void Cancel(CancellationToken cancellationToken)
    {
        _completionSource.TrySetCanceled(cancellationToken);
    }

    /// <summary>
    ///     Resumes the in-flight pause with the supplied <paramref name="decision" />.
    /// </summary>
    /// <param name="decision">The decision to forward to the proxy pipeline.</param>
    public void ResumeWith(BreakpointDecision decision)
    {
        _completionSource.TrySetResult(decision);
    }

    /// <summary>
    ///     Awaits resolution of the pause and returns the decision.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    /// <returns>The user's decision once the pause has been resolved.</returns>
    public Task<BreakpointDecision> WaitForDecisionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _completionSource.Task.WaitAsync(cancellationToken);
    }
}
