using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the Map Local tool window
///     (<c>docs/DESIGN.md § 6.5 Map Local</c>): enable/disable, add an entry
///     with status / reason / headers / body, remove an entry, reject
///     non-numeric or out-of-range status codes.
/// </summary>
public sealed class MapLocalViewModelEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task IsEnabled_FreshViewModel_ReflectsRuleState()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapLocalRule(priority: 300, isEnabled: false);
            using var vm = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);

            await Assert.That(vm.IsEnabled).IsFalse();
            await Assert.That(vm.Entries.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task IsEnabled_SetTrue_PropagatesToRule()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapLocalRule(priority: 300, isEnabled: false);
            using var vm = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);

            vm.IsEnabled = true;

            await Assert.That(rule.IsEnabled).IsTrue();
        });
    }

    [Test]
    public async Task AddEntryCommand_ValidPattern_AddsAndResetsEditor()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
            using var vm = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "https://api.example.com/users";
            vm.NewPatternKind = MatchingRuleKind.Exact;
            vm.ResponseStatusCode = "200";
            vm.ResponseReasonPhrase = "OK";
            vm.ResponseHeaders = "Content-Type: application/json";
            vm.ResponseBody = "{\"users\": []}";

            vm.AddEntryCommand.Execute(null);

            await Assert.That(vm.Entries.Count).IsEqualTo(1);
            var entry = vm.Entries[0].Entry;
            await Assert.That(entry.MatchingRule.Pattern).IsEqualTo("https://api.example.com/users");
            await Assert.That(entry.StatusCode).IsEqualTo(200);
            await Assert.That(entry.ReasonPhrase).IsEqualTo("OK");
            await Assert.That(Encoding.UTF8.GetString(entry.Body.ToArray())).IsEqualTo("{\"users\": []}");

            // The editor must reset so the next entry starts clean.
            await Assert.That(vm.NewPatternText).IsEqualTo(string.Empty);
            await Assert.That(vm.ResponseHeaders).IsEqualTo(string.Empty);
            await Assert.That(vm.ResponseBody).IsEqualTo(string.Empty);
        });
    }

    [Test]
    public async Task AddEntryCommand_NonNumericStatusCode_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
            using var vm = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "https://api.example.com/x";
            vm.ResponseStatusCode = "not-a-number";

            vm.AddEntryCommand.Execute(null);

            await Assert.That(vm.Entries.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task AddEntryCommand_OutOfRangeStatusCode_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
            using var vm = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "https://api.example.com/x";
            vm.ResponseStatusCode = "99";

            vm.AddEntryCommand.Execute(null);

            vm.ResponseStatusCode = "600";
            vm.AddEntryCommand.Execute(null);

            await Assert.That(vm.Entries.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task AddEntryCommand_WhitespacePattern_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
            using var vm = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "  ";

            vm.AddEntryCommand.Execute(null);

            await Assert.That(vm.Entries.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task RemoveEntryCommand_WithSpecificEntry_RemovesIt()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
            using var vm = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "https://api.example.com/x";
            vm.ResponseStatusCode = "200";
            vm.AddEntryCommand.Execute(null);
            vm.NewPatternText = "https://api.example.com/y";
            vm.ResponseStatusCode = "201";
            vm.AddEntryCommand.Execute(null);
            await Assert.That(vm.Entries.Count).IsEqualTo(2);

            var first = vm.Entries.First(e => e.Entry.MatchingRule.Pattern == "https://api.example.com/x");
            vm.RemoveEntryCommand.Execute(first);

            await Assert.That(vm.Entries.Count).IsEqualTo(1);
            await Assert.That(vm.Entries[0].Entry.MatchingRule.Pattern).IsEqualTo("https://api.example.com/y");
        });
    }

    [Test]
    public async Task RemoveEntryCommand_NullEntry_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapLocalRule(priority: 300, isEnabled: true);
            using var vm = new MapLocalViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "https://api.example.com/x";
            vm.ResponseStatusCode = "200";
            vm.AddEntryCommand.Execute(null);

            vm.RemoveEntryCommand.Execute(null);

            await Assert.That(vm.Entries.Count).IsEqualTo(1);
        });
    }
}
