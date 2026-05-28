using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="BlockListViewModel" />.
/// </summary>
public sealed class BlockListViewModelTests
{
    /// <summary>
    ///     After construction, the view model exposes the current state of the rule.
    /// </summary>
    [Test]
    public async Task Constructor_InitialState_ReflectsRule()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));

        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(viewModel.Patterns.Count).IsEqualTo(1);
        await Assert.That(viewModel.Patterns[0].Pattern).IsEqualTo("https://example.com/*");
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting IsEnabled propagates to the underlying rule.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToFalse_DisablesRule()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.IsEnabled = false;

        await Assert.That(rule.IsEnabled).IsFalse();
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting IsEnabled to the current value is a no-op.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToSameValue_NoOpOnRule()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var changeCount = 0;
        rule.Changed += () => changeCount++;

        viewModel.IsEnabled = true;

        await Assert.That(changeCount).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Add pattern command appends the pattern to the rule and clears the editor text.
    /// </summary>
    [Test]
    public async Task AddPatternCommand_ValidInput_AddsPatternAndClearsText()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance)
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
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance)
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
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));
        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);
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
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));
        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);

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
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);

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
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.Dispose();
        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));

        await Assert.That(viewModel.Patterns.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that an external disable of an initially enabled rule propagates to the view
    ///     model via ReloadPatterns and that OnIsEnabledChanged short-circuits when the rule
    ///     already matches the new value.
    /// </summary>
    [Test]
    public async Task ExternalDisable_PropagatesToViewModel_DoesNotLoopBackToRule()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var viewModel = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var changeCountBefore = 0;
        rule.Changed += () => changeCountBefore++;

        rule.SetEnabled(false);

        await Assert.That(viewModel.IsEnabled).IsFalse();
        await Assert.That(changeCountBefore).IsEqualTo(1);
        viewModel.Dispose();
    }
}
