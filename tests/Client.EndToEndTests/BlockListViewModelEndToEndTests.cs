using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the Block List tool window
///     (<c>docs/DESIGN.md § 6.10</c>): enable/disable the rule, add a pattern,
///     remove a pattern, whitespace input handling.
/// </summary>
public sealed class BlockListViewModelEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task IsEnabled_FreshViewModel_ReflectsRuleState()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableBlockListRule(priority: 200, isEnabled: false);
            using var vm = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);

            await Assert.That(vm.IsEnabled).IsFalse();
            await Assert.That(vm.Patterns.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task IsEnabled_SetTrue_PropagatesToRule()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableBlockListRule(priority: 200, isEnabled: false);
            using var vm = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);

            vm.IsEnabled = true;

            await Assert.That(rule.IsEnabled).IsTrue();
        });
    }

    [Test]
    public async Task AddPatternCommand_NonEmptyText_AddsToRuleAndResetsTextBox()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableBlockListRule(priority: 200, isEnabled: true);
            using var vm = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "ads.example.com";
            vm.NewPatternKind = MatchingRuleKind.Exact;

            vm.AddPatternCommand.Execute(null);

            await Assert.That(vm.Patterns.Count).IsEqualTo(1);
            await Assert.That(vm.Patterns[0].Rule.Pattern).IsEqualTo("ads.example.com");
            await Assert.That(vm.NewPatternText).IsEqualTo(string.Empty);
        });
    }

    [Test]
    public async Task AddPatternCommand_WhitespaceText_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableBlockListRule(priority: 200, isEnabled: true);
            using var vm = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "  ";

            vm.AddPatternCommand.Execute(null);

            await Assert.That(vm.Patterns.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task RemovePatternCommand_WithSpecificEntry_RemovesEntryFromRule()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableBlockListRule(priority: 200, isEnabled: true);
            rule.AddPattern(new MatchingRule("tracker.example.com", MatchingRuleKind.Exact));
            rule.AddPattern(new MatchingRule("*.ads.com", MatchingRuleKind.Wildcard));
            using var vm = new BlockListViewModel(rule, InlineUserInterfaceScheduler.Instance);
            await Assert.That(vm.Patterns.Count).IsEqualTo(2);

            var first = vm.Patterns.First(p => p.Rule.Pattern == "tracker.example.com");
            vm.RemovePatternCommand.Execute(first);

            await Assert.That(vm.Patterns.Count).IsEqualTo(1);
            await Assert.That(vm.Patterns[0].Rule.Pattern).IsEqualTo("*.ads.com");
        });
    }
}
