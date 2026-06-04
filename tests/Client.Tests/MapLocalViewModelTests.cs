using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="MapLocalViewModel" />.
/// </summary>
public sealed class MapLocalViewModelTests
{
    /// <summary>
    ///     After construction, the view model exposes the current state of the rule.
    /// </summary>
    [Test]
    public async Task Constructor_RuleHasEntry_PublishesEntry()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        rule.AddEntry(new MapLocalEntry
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = new List<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = matchingRule,
            ReasonPhrase = "OK",
            StatusCode = 200,
        });

        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(viewModel.Entries.Count).IsEqualTo(1);
        await Assert.That(viewModel.Entries[0].Status).IsEqualTo("200 OK");
        viewModel.Dispose();
    }

    /// <summary>
    ///     Setting IsEnabled propagates to the underlying rule.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetToFalse_DisablesRule()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);

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
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var changeCount = 0;
        rule.Changed += () => changeCount++;

        viewModel.IsEnabled = true;

        await Assert.That(changeCount).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     AddEntry command appends a new entry to the rule and clears editor text.
    /// </summary>
    [Test]
    public async Task AddEntryCommand_ValidInput_AddsEntryAndClearsEditor()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "https://example.com/*",
            NewPatternKind = MatchingRuleKind.Wildcard,
            ResponseStatusCode = "201",
            ResponseReasonPhrase = "Created",
            ResponseHeaders = "Content-Type: application/json\nX-Trace: abc",
            ResponseBody = "{}",
        };
        viewModel.ValidationMessage = "Previous error";

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(1);
        var entry = rule.GetEntries()[0];
        await Assert.That(entry.StatusCode).IsEqualTo(201);
        await Assert.That(entry.Headers.Count).IsEqualTo(2);
        await Assert.That(viewModel.ValidationMessage).IsNull();
        await Assert.That(viewModel.NewPatternText).IsEqualTo(string.Empty);
        await Assert.That(viewModel.ResponseBody).IsEqualTo(string.Empty);
        await Assert.That(viewModel.ResponseHeaders).IsEqualTo(string.Empty);
        viewModel.Dispose();
    }

    /// <summary>
    ///     AddEntry command with an invalid regex reports a validation message and preserves editor state.
    /// </summary>
    [Test]
    public async Task AddEntryCommand_InvalidRegex_SetsValidationMessageWithoutMutatingRule()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "([unterminated",
            NewPatternKind = MatchingRuleKind.Regex,
            ResponseStatusCode = "201",
            ResponseReasonPhrase = "Created",
            ResponseHeaders = "Content-Type: application/json",
            ResponseBody = "{}",
        };

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(0);
        await Assert.That(viewModel.ValidationMessage).IsEqualTo("Pattern must be a valid regular expression.");
        await Assert.That(viewModel.NewPatternText).IsEqualTo("([unterminated");
        await Assert.That(viewModel.ResponseBody).IsEqualTo("{}");
        await Assert.That(viewModel.ResponseHeaders).IsEqualTo("Content-Type: application/json");
        viewModel.Dispose();
    }

    /// <summary>
    ///     AddEntry command with empty pattern is a no-op.
    /// </summary>
    [Test]
    public async Task AddEntryCommand_EmptyPattern_IsNoOp()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "   ",
        };

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     AddEntry command with non-integer status code is a no-op.
    /// </summary>
    [Test]
    public async Task AddEntryCommand_NonIntegerStatus_IsNoOp()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "https://example.com/*",
            ResponseStatusCode = "abc",
        };

        viewModel.AddEntryCommand.Execute(null);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     AddEntry command with out-of-range status code is a no-op.
    /// </summary>
    [Test]
    public async Task AddEntryCommand_OutOfRangeStatus_IsNoOp()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance)
        {
            NewPatternText = "https://example.com/*",
            ResponseStatusCode = "999",
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
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        rule.AddEntry(new MapLocalEntry
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = new List<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = matchingRule,
            ReasonPhrase = "OK",
            StatusCode = 200,
        });
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);
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
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        rule.AddEntry(new MapLocalEntry
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = new List<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = matchingRule,
            ReasonPhrase = "OK",
            StatusCode = 200,
        });
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.RemoveEntryCommand.Execute(null);

        await Assert.That(rule.GetEntries().Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     After disposal, subsequent rule mutations do not update the view model.
    /// </summary>
    [Test]
    public async Task Dispose_AfterDispose_UnsubscribesFromChangedEvent()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);

        viewModel.Dispose();

        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        rule.AddEntry(new MapLocalEntry
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = new List<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = matchingRule,
            ReasonPhrase = "OK",
            StatusCode = 200,
        });

        await Assert.That(viewModel.Entries.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that an external disable of an initially enabled rule propagates to the view
    ///     model via ReloadEntries, and that OnIsEnabledChanged short-circuits when the rule
    ///     already matches the new value.
    /// </summary>
    [Test]
    public async Task ExternalDisable_PropagatesToViewModel_DoesNotLoopBackToRule()
    {
        var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
        var viewModel = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);
        var changeCountBefore = 0;
        rule.Changed += () => changeCountBefore++;

        rule.SetEnabled(false);

        await Assert.That(viewModel.IsEnabled).IsFalse();
        await Assert.That(changeCountBefore).IsEqualTo(1);
        viewModel.Dispose();
    }
}
