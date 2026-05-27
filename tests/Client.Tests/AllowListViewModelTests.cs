using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="AllowListViewModel" />.
/// </summary>
public sealed class AllowListViewModelTests
{
    /// <summary>
    ///     After construction, the view model exposes the current state of the rule.
    /// </summary>
    [Test]
    public async Task Constructor_InitialState_ReflectsRule()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));

        var viewModel = new AllowListViewModel(rule, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.IsEnabled).IsFalse();
        await Assert.That(viewModel.Patterns.Count).IsEqualTo(1);
        await Assert.That(viewModel.Patterns[0].Pattern).IsEqualTo("https://example.com/*");
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting IsEnabled propagates to the underlying rule.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToTrue_EnablesRule()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        var viewModel = new AllowListViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.IsEnabled = true;

        await Assert.That(rule.IsEnabled).IsTrue();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting IsEnabled to the current value is a no-op.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToSameValue_NoOpOnRule()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        var viewModel = new AllowListViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var changeCount = 0;
        rule.Changed += () => changeCount++;

        viewModel.IsEnabled = false;

        await Assert.That(changeCount).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Add pattern command appends the pattern to the rule and clears the editor text.
    /// </summary>
    [Test]
    public async Task AddPatternCommand_ValidInput_AddsPatternAndClearsText()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        var viewModel = new AllowListViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "https://example.com/*",
            NewPatternKind = MatchingRuleKind.Wildcard,
        };

        viewModel.AddPatternCommand.Execute(null);

        await Assert.That(rule.GetPatterns().Count).IsEqualTo(1);
        await Assert.That(viewModel.NewPatternText).IsEqualTo(string.Empty);
        await Assert.That(viewModel.Patterns.Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Add pattern command with empty text is a no-op.
    /// </summary>
    [Test]
    public async Task AddPatternCommand_EmptyText_IsNoOp()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        var viewModel = new AllowListViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "   ",
        };

        viewModel.AddPatternCommand.Execute(null);

        await Assert.That(rule.GetPatterns().Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Remove pattern command removes the pattern from the rule and from the observable collection.
    /// </summary>
    [Test]
    public async Task RemovePatternCommand_RegisteredPattern_RemovesPattern()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));
        var viewModel = new AllowListViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var entry = viewModel.Patterns[0];

        viewModel.RemovePatternCommand.Execute(entry);

        await Assert.That(rule.GetPatterns().Count).IsEqualTo(0);
        await Assert.That(viewModel.Patterns.Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Remove pattern command with null entry is a no-op.
    /// </summary>
    [Test]
    public async Task RemovePatternCommand_NullEntry_IsNoOp()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));
        var viewModel = new AllowListViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.RemovePatternCommand.Execute(null);

        await Assert.That(rule.GetPatterns().Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The view model reloads patterns when the rule's Changed event fires.
    /// </summary>
    [Test]
    public async Task RuleChanged_OutOfBandUpdate_RefreshesPatternsCollection()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        var viewModel = new AllowListViewModel(rule, InlineUserInterfaceScheduler.Instance);

        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));

        await Assert.That(viewModel.Patterns.Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     After disposal, subsequent rule mutations do not throw or update the view model.
    /// </summary>
    [Test]
    public async Task Dispose_AfterDispose_UnsubscribesFromChangedEvent()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        var viewModel = new AllowListViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.Dispose();
        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));

        await Assert.That(viewModel.Patterns.Count).IsEqualTo(0);
    }
}
