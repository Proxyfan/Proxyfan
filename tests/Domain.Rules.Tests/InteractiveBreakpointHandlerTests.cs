using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="InteractiveBreakpointHandler" />.
/// </summary>
public sealed class InteractiveBreakpointHandlerTests
{
    /// <summary>
    ///     When the configuration is disabled the handler resumes immediately without
    ///     creating a pause.
    /// </summary>
    [Test]
    public async Task ResolveRequestAsync_Disabled_ResumesImmediately()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: false);
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        var request = NewRequest("https://example.com/");

        var decision = await handler.ResolveRequestAsync(request, CancellationToken.None);

        await Assert.That(decision.IsAborting).IsFalse();
        await Assert.That(decision.ModifiedRequest).IsSameReferenceAs(request);
        await Assert.That(inbox.GetPending().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     When the configuration is enabled but no pattern matches, the handler resumes
    ///     immediately and does not enqueue a pause.
    /// </summary>
    [Test]
    public async Task ResolveRequestAsync_NoMatch_ResumesImmediately()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("https://nope.example.com/*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        var request = NewRequest("https://example.com/");

        var decision = await handler.ResolveRequestAsync(request, CancellationToken.None);

        await Assert.That(decision.ModifiedRequest).IsSameReferenceAs(request);
        await Assert.That(inbox.GetPending().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     When a matching pattern exists the handler enqueues a pause and awaits the inbox's resolution.
    /// </summary>
    [Test]
    public async Task ResolveRequestAsync_MatchingPattern_EnqueuesPauseAndAwaitsResolution()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        var request = NewRequest("https://example.com/");
        var modified = NewRequest("https://example.com/modified");
        BreakpointPause? enqueued = null;
        inbox.PauseAdded += p =>
        {
            enqueued = p;
            inbox.Resolve(p, BreakpointDecisions.ResumeRequest(modified));
        };

        var decision = await handler.ResolveRequestAsync(request, CancellationToken.None);

        await Assert.That(enqueued).IsNotNull();
        await Assert.That(decision.ModifiedRequest).IsSameReferenceAs(modified);
        await Assert.That(inbox.GetPending().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Cancellation aborts a pending pause and re-throws OperationCanceledException.
    /// </summary>
    [Test]
    public async Task ResolveRequestAsync_Cancelled_AbortsPendingPause()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        using var cts = new CancellationTokenSource();
        var task = handler.ResolveRequestAsync(NewRequest("https://example.com/"), cts.Token);

        cts.Cancel();

        await Assert.That(async () => await task).Throws<OperationCanceledException>();
        await Assert.That(inbox.GetPending().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     A pre-cancelled token must short-circuit the pause without exposing it to the inbox,
    ///     so a synchronous subscriber cannot beat the cancellation.
    /// </summary>
    [Test]
    public async Task ResolveRequestAsync_PreCancelledToken_DoesNotEnqueuePause()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        var addedPauses = 0;
        inbox.PauseAdded += p =>
        {
            addedPauses++;
            inbox.Resolve(p, BreakpointDecisions.ResumeRequest(p.Request));
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(async () => await handler.ResolveRequestAsync(NewRequest("https://example.com/"), cts.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(addedPauses).IsEqualTo(0);
        await Assert.That(inbox.GetPending().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     When back-pressure is enabled and the queue is full, new matches bypass the breakpoint.
    /// </summary>
    [Test]
    public async Task ResolveRequestAsync_BackPressureEnabled_QueueFull_SkipsBreakpoint()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.SetMaxPendingPauses(1);
        configuration.SetBackPressureEnabled(isBackPressureEnabled: true);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        var firstRequest = NewRequest("https://example.com/first");
        var secondRequest = NewRequest("https://example.com/second");
        var secondModifiedRequest = NewRequest("https://example.com/second-modified");
        var firstPauseAdded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        inbox.PauseAdded += _ => firstPauseAdded.TrySetResult();
        var firstTask = handler.ResolveRequestAsync(firstRequest, CancellationToken.None);
        await firstPauseAdded.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var secondDecision = await handler.ResolveRequestAsync(secondRequest, CancellationToken.None);

        await Assert.That(secondDecision.ModifiedRequest).IsSameReferenceAs(secondRequest);
        await Assert.That(inbox.PendingCount).IsEqualTo(1);

        var pending = inbox.GetPending();
        await Assert.That(pending.Count).IsEqualTo(1);
        inbox.Resolve(pending[0], BreakpointDecisions.ResumeRequest(secondModifiedRequest));
        _ = await firstTask.ConfigureAwait(false);
    }

    /// <summary>
    ///     When pending pauses reach the cap and back-pressure is disabled, the oldest
    ///     pause is auto-resumed to make room for a new pause.
    /// </summary>
    [Test]
    public async Task ResolveRequestAsync_QueueFull_DropsOldestAndResumes()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.SetMaxPendingPauses(1);
        configuration.SetBackPressureEnabled(isBackPressureEnabled: false);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        var firstRequest = NewRequest("https://example.com/first");
        var secondRequest = NewRequest("https://example.com/second");
        var secondModifiedRequest = NewRequest("https://example.com/second-modified");
        var firstPauseAdded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        inbox.PauseAdded += _ => firstPauseAdded.TrySetResult();

        var firstTask = handler.ResolveRequestAsync(firstRequest, CancellationToken.None);
        await firstPauseAdded.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var secondTask = handler.ResolveRequestAsync(secondRequest, CancellationToken.None);

        var firstDecision = await firstTask.ConfigureAwait(false);
        await Assert.That(firstDecision.ModifiedRequest).IsSameReferenceAs(firstRequest);

        var pending = inbox.GetPending();
        await Assert.That(pending.Count).IsEqualTo(1);
        inbox.Resolve(pending[0], BreakpointDecisions.ResumeRequest(secondModifiedRequest));
        var secondDecision = await secondTask.ConfigureAwait(false);
        await Assert.That(secondDecision.ModifiedRequest).IsSameReferenceAs(secondModifiedRequest);
    }

    /// <summary>
    ///     Unresolved pauses are automatically resumed after the configured timeout.
    /// </summary>
    [Test]
    public async Task ResolveRequestAsync_TimeoutElapsed_AutoResumes()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.SetPauseTimeout(TimeSpan.FromMilliseconds(30));
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        var request = NewRequest("https://example.com/timeout");

        var decision = await handler.ResolveRequestAsync(request, CancellationToken.None);

        await Assert.That(decision.ModifiedRequest).IsSameReferenceAs(request);
        await Assert.That(inbox.PendingCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Response-phase resolution resumes immediately when the configuration is disabled.
    /// </summary>
    [Test]
    public async Task ResolveResponseAsync_Disabled_ResumesImmediately()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: false);
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        var request = NewRequest("https://example.com/");
        var response = NewResponse();

        var decision = await handler.ResolveResponseAsync(request, response, CancellationToken.None);

        await Assert.That(decision.ModifiedResponse).IsSameReferenceAs(response);
    }

    /// <summary>
    ///     Response-phase resolution enqueues a pause when a pattern matches.
    /// </summary>
    [Test]
    public async Task ResolveResponseAsync_MatchingPattern_EnqueuesPauseAndAwaitsResolution()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();
        var handler = new InteractiveBreakpointHandler(configuration, inbox);
        var request = NewRequest("https://example.com/");
        var response = NewResponse();
        var modified = NewResponse(status: 201);
        inbox.PauseAdded += p => inbox.Resolve(p, BreakpointDecisions.ResumeResponse(modified));

        var decision = await handler.ResolveResponseAsync(request, response, CancellationToken.None);

        await Assert.That(decision.ModifiedResponse).IsSameReferenceAs(modified);
    }

    private static HypertextTransferProtocolRequestData NewRequest(string url)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", new Uri(url).Host),
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData NewResponse(int status = 200)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = status,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }

}
