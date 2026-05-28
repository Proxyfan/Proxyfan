using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="BreakpointViewModel" />.
/// </summary>
public sealed class BreakpointViewModelTests
{
    /// <summary>
    ///     After construction, the view model exposes the current configuration state.
    /// </summary>
    [Test]
    public async Task Constructor_InitialState_ReflectsConfiguration()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();

        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(viewModel.Patterns.Count).IsEqualTo(1);
        await Assert.That(viewModel.Patterns[0].Pattern).IsEqualTo("https://example.com/*");
        await Assert.That(viewModel.Pauses.Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting IsEnabled propagates to the underlying configuration.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToFalse_DisablesConfiguration()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);

        viewModel.IsEnabled = false;

        await Assert.That(configuration.IsEnabled).IsFalse();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting Phases propagates to the underlying configuration.
    /// </summary>
    [Test]
    public async Task Phases_SetToRequestOnly_UpdatesConfiguration()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);

        viewModel.Phases = BreakpointPhase.Request;

        await Assert.That(configuration.Phases).IsEqualTo(BreakpointPhase.Request);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Add pattern command appends the pattern to the configuration and clears the editor text.
    /// </summary>
    [Test]
    public async Task AddPatternCommand_ValidInput_AddsPatternAndClearsText()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "https://api.example.com/*",
            NewPatternKind = MatchingRuleKind.Wildcard,
        };

        viewModel.AddPatternCommand.Execute(null);

        await Assert.That(configuration.GetPatterns().Count).IsEqualTo(1);
        await Assert.That(viewModel.NewPatternText).IsEqualTo(string.Empty);
        await Assert.That(viewModel.Patterns.Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Add pattern command with whitespace text is a no-op.
    /// </summary>
    [Test]
    public async Task AddPatternCommand_WhitespaceInput_DoesNotAddPattern()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "   ",
        };

        viewModel.AddPatternCommand.Execute(null);

        await Assert.That(configuration.GetPatterns().Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     RemovePattern command removes the supplied entry from the configuration.
    /// </summary>
    [Test]
    public async Task RemovePatternCommand_KnownEntry_RemovesPattern()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        configuration.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        var entry = viewModel.Patterns[0];

        viewModel.RemovePatternCommand.Execute(entry);

        await Assert.That(configuration.GetPatterns().Count).IsEqualTo(0);
        await Assert.That(viewModel.Patterns.Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     When the inbox raises PauseAdded, the view model appends a wrapper and selects it.
    /// </summary>
    [Test]
    public async Task InboxPauseAdded_FirstPause_AppendsAndSelects()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        var pause = BreakpointPauseFactory.CreateRequest("https://example.com/users");

        inbox.Add(pause);

        await Assert.That(viewModel.Pauses.Count).IsEqualTo(1);
        await Assert.That(viewModel.SelectedPause).IsNotNull();
        await Assert.That(viewModel.SelectedPause!.RequestUrl).IsEqualTo("https://example.com/users");
        viewModel.Dispose();
    }

    /// <summary>
    ///     When the inbox raises PauseResolved for the selected pause, it clears the selection
    ///     and removes the entry from the list.
    /// </summary>
    [Test]
    public async Task InboxPauseResolved_SelectedPause_RemovesAndClearsSelection()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        var pause = BreakpointPauseFactory.CreateRequest("https://example.com/users");
        inbox.Add(pause);

        inbox.Resolve(pause, BreakpointDecisions.ResumeRequest(pause.Request));

        await Assert.That(viewModel.Pauses.Count).IsEqualTo(0);
        await Assert.That(viewModel.SelectedPause).IsNull();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Resume command on a request-phase pause forwards a request-phase decision through the inbox.
    /// </summary>
    [Test]
    public async Task ResumeCommand_RequestPhasePause_ResumesWithEditedRequest()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        var pause = BreakpointPauseFactory.CreateRequest("https://example.com/users");
        inbox.Add(pause);
        viewModel.SelectedPause!.RequestUrl = "https://example.com/admins";
        viewModel.SelectedPause!.HeadersText = "X-Test: enabled\n";
        viewModel.SelectedPause!.BodyText = "hello";

        viewModel.ResumeCommand.Execute(viewModel.SelectedPause);

        var decision = await pause.WaitForDecisionAsync(CancellationToken.None);
        await Assert.That(decision.IsAborting).IsFalse();
        await Assert.That(decision.ModifiedRequest).IsNotNull();
        await Assert.That(decision.ModifiedRequest!.RequestUri.AbsoluteUri).IsEqualTo("https://example.com/admins");
        await Assert.That(decision.ModifiedRequest.Headers.Get("X-Test")).IsEqualTo("enabled");
        await Assert.That(Encoding.UTF8.GetString(decision.ModifiedRequest.Body.Span)).IsEqualTo("hello");
        viewModel.Dispose();
    }

    /// <summary>
    ///     Resume command on a response-phase pause forwards a response-phase decision through the inbox.
    /// </summary>
    [Test]
    public async Task ResumeCommand_ResponsePhasePause_ResumesWithEditedResponse()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        var pause = BreakpointPauseFactory.CreateResponse("https://example.com/users");
        inbox.Add(pause);
        viewModel.SelectedPause!.StatusCode = 418;
        viewModel.SelectedPause!.ReasonPhrase = "I'm a teapot";
        viewModel.SelectedPause!.BodyText = "tea";

        viewModel.ResumeCommand.Execute(viewModel.SelectedPause);

        var decision = await pause.WaitForDecisionAsync(CancellationToken.None);
        await Assert.That(decision.IsAborting).IsFalse();
        await Assert.That(decision.ModifiedResponse).IsNotNull();
        await Assert.That(decision.ModifiedResponse!.StatusCode).IsEqualTo(418);
        await Assert.That(decision.ModifiedResponse.ReasonPhrase).IsEqualTo("I'm a teapot");
        await Assert.That(Encoding.UTF8.GetString(decision.ModifiedResponse.Body.Span)).IsEqualTo("tea");
        viewModel.Dispose();
    }

    /// <summary>
    ///     Abort command forwards an abort decision through the inbox.
    /// </summary>
    [Test]
    public async Task AbortCommand_AnyPause_AbortsThroughInbox()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        var pause = BreakpointPauseFactory.CreateRequest("https://example.com/users");
        inbox.Add(pause);

        viewModel.AbortCommand.Execute(viewModel.SelectedPause);

        var decision = await pause.WaitForDecisionAsync(CancellationToken.None);
        await Assert.That(decision.IsAborting).IsTrue();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Resume and Abort commands handle null parameters gracefully.
    /// </summary>
    [Test]
    public async Task ResumeAndAbortCommands_NullParameter_DoesNotThrow()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);

        viewModel.ResumeCommand.Execute(null);
        viewModel.AbortCommand.Execute(null);
        viewModel.RemovePatternCommand.Execute(null);

        await Assert.That(viewModel.Pauses.Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The Dispose method unsubscribes from the configuration and inbox events.
    /// </summary>
    [Test]
    public async Task Dispose_AfterCall_StopsObservingEvents()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);

        viewModel.Dispose();
        var pause = BreakpointPauseFactory.CreateRequest("https://example.com/x");
        inbox.Add(pause);
        configuration.AddPattern(new MatchingRule("https://*", MatchingRuleKind.Wildcard));

        await Assert.That(viewModel.Pauses.Count).IsEqualTo(0);
        await Assert.That(viewModel.Patterns.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Assigning IsEnabled to its current value short-circuits without notifying the configuration.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToSameValue_DoesNotPropagateToConfiguration()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        var changedCount = 0;
        configuration.Changed += () => changedCount++;

        viewModel.IsEnabled = true;

        await Assert.That(configuration.IsEnabled).IsTrue();
        await Assert.That(changedCount).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Assigning Phases to its current value short-circuits without notifying the configuration.
    /// </summary>
    [Test]
    public async Task Phases_SetToSameValue_DoesNotPropagateToConfiguration()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        var changedCount = 0;
        configuration.Changed += () => changedCount++;

        viewModel.Phases = viewModel.Phases;

        await Assert.That(changedCount).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Resolving a pause that is NOT first in the list still removes the correct entry.
    /// </summary>
    [Test]
    public async Task InboxPauseResolved_PauseNotFirstInList_RemovesCorrectEntry()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        var first = BreakpointPauseFactory.CreateRequest("https://example.com/one");
        var second = BreakpointPauseFactory.CreateRequest("https://example.com/two");
        inbox.Add(first);
        inbox.Add(second);
        await Assert.That(viewModel.Pauses.Count).IsEqualTo(2);

        inbox.Resolve(first, BreakpointDecisions.ResumeRequest(first.Request));

        await Assert.That(viewModel.Pauses.Count).IsEqualTo(1);
        await Assert.That(viewModel.Pauses[0].Pause.PauseId).IsEqualTo(second.PauseId);
        viewModel.Dispose();
    }

    /// <summary>
    ///     A configuration whose IsEnabled state is mutated externally is synced into the
    ///     view model on the next Changed notification.
    /// </summary>
    [Test]
    public async Task ConfigurationChanged_ExternalSetEnabledFalse_SyncsIsEnabled()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        await Assert.That(viewModel.IsEnabled).IsTrue();

        configuration.SetEnabled(isEnabled: false);

        await Assert.That(viewModel.IsEnabled).IsFalse();
        viewModel.Dispose();
    }

    /// <summary>
    ///     A configuration whose Phases is mutated externally is synced into the view model
    ///     on the next Changed notification.
    /// </summary>
    [Test]
    public async Task ConfigurationChanged_ExternalSetPhases_SyncsPhases()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);
        await Assert.That(viewModel.Phases).IsEqualTo(BreakpointPhase.Both);

        configuration.SetPhases(BreakpointPhase.Request);

        await Assert.That(viewModel.Phases).IsEqualTo(BreakpointPhase.Request);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Constructing the view model with a pre-populated inbox loads existing pauses and
    ///     auto-selects the first one.
    /// </summary>
    [Test]
    public async Task Constructor_InboxHasPendingPauses_LoadsAndSelectsFirst()
    {
        var configuration = new MutableBreakpointConfiguration(isEnabled: true);
        var inbox = new BreakpointPauseInbox();
        var first = BreakpointPauseFactory.CreateRequest("https://example.com/one");
        var second = BreakpointPauseFactory.CreateRequest("https://example.com/two");
        inbox.Add(first);
        inbox.Add(second);

        var viewModel = new BreakpointViewModel(configuration, inbox, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.Pauses.Count).IsEqualTo(2);
        await Assert.That(viewModel.SelectedPause).IsNotNull();
        await Assert.That(viewModel.SelectedPause!.Pause.PauseId).IsEqualTo(first.PauseId);
        viewModel.Dispose();
    }
}

internal static class BreakpointPauseFactory
{
    public static BreakpointPause CreateRequest(string url)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return new BreakpointPause(Guid.NewGuid(), request);
    }

    public static BreakpointPause CreateResponse(string url)
    {
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(requestParameters);
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);
        return new BreakpointPause(Guid.NewGuid(), request, response);
    }
}
