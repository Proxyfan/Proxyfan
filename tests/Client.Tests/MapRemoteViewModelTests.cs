using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="MapRemoteViewModel" />.
/// </summary>
public sealed class MapRemoteViewModelTests
{
    /// <summary>
    ///     After construction, the view model exposes the current state of the rule.
    /// </summary>
    [Test]
    public async Task Constructor_RuleHasEntry_PublishesEntry()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination("https", "internal.example.com", 8443, "/v2", false);
        rule.AddEntry(new MapRemoteEntry { Destination = destination, IsEnabled = true, MatchingRule = matchingRule });

        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(viewModel.Entries.Count).IsEqualTo(1);
        await Assert.That(viewModel.Entries[0].Pattern).IsEqualTo("https://example.com/*");
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting IsEnabled propagates to the underlying rule.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToFalse_DisablesRule()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.IsEnabled = false;

        await Assert.That(rule.IsEnabled).IsFalse();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting IsEnabled to the current value does not raise a Changed event.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToSameValue_DoesNotRaiseChanged()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var changeCount = 0;
        rule.Changed += () => changeCount++;

        viewModel.IsEnabled = true;

        await Assert.That(changeCount).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     AddEntry command appends a new entry to the rule.
    /// </summary>
    [Test]
    public async Task AddEntryCommand_ValidInput_AddsEntryAndClearsEditor()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "https://example.com/*",
            NewPatternKind = MatchingRuleKind.Wildcard,
            DestinationScheme = "https",
            DestinationHost = "internal.example.com",
            DestinationPort = "8443",
            DestinationPath = "/v2",
            IsPreservingHostHeader = true,
        };

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(1);
        await Assert.That(viewModel.Entries.Count).IsEqualTo(1);
        await Assert.That(viewModel.NewPatternText).IsEqualTo(string.Empty);
        await Assert.That(viewModel.DestinationScheme).IsEqualTo(string.Empty);
        viewModel.Dispose();
    }

    /// <summary>
    ///     AddEntry command with whitespace pattern text is a no-op.
    /// </summary>
    [Test]
    public async Task AddEntryCommand_WhitespacePattern_IsNoOp()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "   ",
        };

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     AddEntry command with non-integer port is a no-op.
    /// </summary>
    [Test]
    public async Task AddEntryCommand_NonIntegerPort_IsNoOp()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "https://example.com/*",
            DestinationPort = "not-a-number",
        };

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     RemoveEntry command removes the entry from the rule and the observable collection.
    /// </summary>
    [Test]
    public async Task RemoveEntryCommand_RegisteredEntry_RemovesEntry()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(null, "internal.example.com", null, null, false);
        rule.AddEntry(new MapRemoteEntry { Destination = destination, IsEnabled = true, MatchingRule = matchingRule });
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var entry = viewModel.Entries[0];

        viewModel.RemoveEntryCommand.Execute(entry);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(0);
        await Assert.That(viewModel.Entries.Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     RemoveEntry command with a null entry is a no-op.
    /// </summary>
    [Test]
    public async Task RemoveEntryCommand_NullEntry_IsNoOp()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(null, "internal.example.com", null, null, false);
        rule.AddEntry(new MapRemoteEntry { Destination = destination, IsEnabled = true, MatchingRule = matchingRule });
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.RemoveEntryCommand.Execute(null);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The view model reloads entries when the rule's Changed event fires.
    /// </summary>
    [Test]
    public async Task RuleChanged_OutOfBandUpdate_RefreshesEntriesCollection()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(null, "internal.example.com", null, null, false);

        rule.AddEntry(new MapRemoteEntry { Destination = destination, IsEnabled = true, MatchingRule = matchingRule });

        await Assert.That(viewModel.Entries.Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     After disposal, subsequent rule mutations do not update the view model.
    /// </summary>
    [Test]
    public async Task Dispose_AfterDispose_UnsubscribesFromChangedEvent()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.Dispose();

        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(null, "internal.example.com", null, null, false);
        rule.AddEntry(new MapRemoteEntry { Destination = destination, IsEnabled = true, MatchingRule = matchingRule });

        await Assert.That(viewModel.Entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     AddEntry with a blank DestinationPath uses null path on the destination
    ///     (covers the true branch of the path ternary in AddEntry).
    /// </summary>
    [Test]
    public async Task AddEntryCommand_BlankDestinationPath_UsesNullPath()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "https://example.com/*",
            NewPatternKind = MatchingRuleKind.Wildcard,
            DestinationHost = "internal.example.com",
            DestinationPath = "   ",
        };

        viewModel.AddEntryCommand.Execute(null);

        var added = rule.GetEntries()[0];
        await Assert.That(added.Destination.Path).IsNull();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting IsEnabled to the same value the rule already has is a no-op
    ///     (covers the true branch of the equality check in OnIsEnabledChanged).
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToSameValueAsRule_DoesNotMutateRule()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var initialEnabled = rule.IsEnabled;

        viewModel.IsEnabled = true;

        await Assert.That(rule.IsEnabled).IsEqualTo(initialEnabled);
        viewModel.Dispose();
    }

    /// <summary>
    ///     When a rule mutation propagates while IsEnabled is in sync, the view model
    ///     does not reassign IsEnabled (covers the false branch of the IsEnabled
    ///     equality check in ReloadEntries).
    /// </summary>
    [Test]
    public async Task RuleChanged_WhenIsEnabledMatches_DoesNotReassignIsEnabled()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: true);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(null, "internal.example.com", null, null, false);

        rule.AddEntry(new MapRemoteEntry { Destination = destination, IsEnabled = true, MatchingRule = matchingRule });

        await Assert.That(viewModel.Entries.Count).IsEqualTo(1);
        await Assert.That(viewModel.IsEnabled).IsTrue();
        viewModel.Dispose();
    }

    /// <summary>
    ///     When the rule's IsEnabled changes externally, ReloadEntries propagates the
    ///     new value into the view model. The subsequent OnIsEnabledChanged hits the
    ///     true branch of the equality check (rule already matches) and does not
    ///     re-enter SetEnabled.
    /// </summary>
    [Test]
    public async Task ExternalEnable_PropagatesToViewModel_DoesNotLoopBackToRule()
    {
        var rule = new MutableMapRemoteRule(priority: 200, isEnabled: false);
        var viewModel = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var changedInvocations = 0;
        rule.Changed += () => changedInvocations++;

        rule.SetEnabled(true);

        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(rule.IsEnabled).IsTrue();
        await Assert.That(changedInvocations).IsEqualTo(1);
        viewModel.Dispose();
    }
}
