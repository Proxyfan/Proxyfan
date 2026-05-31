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
///     End-to-end UI tests covering the Map Remote tool window
///     (<c>docs/DESIGN.md § 6.6 Map Remote</c>): enable/disable, add an entry
///     with destination scheme/host/port/path, the preserve-host-header toggle,
///     remove an entry, reject non-numeric ports.
/// </summary>
public sealed class MapRemoteViewModelEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task IsEnabled_FreshViewModel_ReflectsRuleState()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapRemoteRule(priority: 350, isEnabled: false);
            using var vm = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);

            await Assert.That(vm.IsEnabled).IsFalse();
            await Assert.That(vm.Entries.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task IsEnabled_SetTrue_PropagatesToRule()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapRemoteRule(priority: 350, isEnabled: false);
            using var vm = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);

            vm.IsEnabled = true;

            await Assert.That(rule.IsEnabled).IsTrue();
        });
    }

    [Test]
    public async Task AddEntryCommand_AllFieldsPopulated_AddsAndResetsEditor()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapRemoteRule(priority: 350, isEnabled: true);
            using var vm = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "*.example.com";
            vm.NewPatternKind = MatchingRuleKind.Wildcard;
            vm.DestinationScheme = "https";
            vm.DestinationHost = "staging.example.com";
            vm.DestinationPort = "443";
            vm.DestinationPath = "/api/v2";
            vm.IsPreservingHostHeader = true;

            vm.AddEntryCommand.Execute(null);

            await Assert.That(vm.Entries.Count).IsEqualTo(1);
            var entry = vm.Entries[0].Entry;
            await Assert.That(entry.MatchingRule.Pattern).IsEqualTo("*.example.com");
            await Assert.That(entry.Destination.Scheme).IsEqualTo("https");
            await Assert.That(entry.Destination.Host).IsEqualTo("staging.example.com");
            await Assert.That(entry.Destination.Port).IsEqualTo(443);
            await Assert.That(entry.Destination.Path).IsEqualTo("/api/v2");
            await Assert.That(entry.Destination.IsPreservingHostHeader).IsTrue();

            await Assert.That(vm.NewPatternText).IsEqualTo(string.Empty);
            await Assert.That(vm.DestinationScheme).IsEqualTo(string.Empty);
            await Assert.That(vm.DestinationHost).IsEqualTo(string.Empty);
            await Assert.That(vm.DestinationPort).IsEqualTo(string.Empty);
            await Assert.That(vm.DestinationPath).IsEqualTo(string.Empty);
        });
    }

    [Test]
    public async Task AddEntryCommand_OnlyHost_OtherFieldsRemainNull()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapRemoteRule(priority: 350, isEnabled: true);
            using var vm = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "*.example.com";
            vm.DestinationHost = "staging.example.com";

            vm.AddEntryCommand.Execute(null);

            await Assert.That(vm.Entries.Count).IsEqualTo(1);
            var entry = vm.Entries[0].Entry;
            await Assert.That(entry.Destination.Host).IsEqualTo("staging.example.com");
            await Assert.That(entry.Destination.Scheme).IsNull();
            await Assert.That(entry.Destination.Port).IsNull();
            await Assert.That(entry.Destination.Path).IsNull();
        });
    }

    [Test]
    public async Task AddEntryCommand_NonNumericPort_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapRemoteRule(priority: 350, isEnabled: true);
            using var vm = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "*.example.com";
            vm.DestinationHost = "staging.example.com";
            vm.DestinationPort = "not-a-number";

            vm.AddEntryCommand.Execute(null);

            await Assert.That(vm.Entries.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task AddEntryCommand_WhitespacePattern_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapRemoteRule(priority: 350, isEnabled: true);
            using var vm = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "  ";
            vm.DestinationHost = "staging.example.com";

            vm.AddEntryCommand.Execute(null);

            await Assert.That(vm.Entries.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task RemoveEntryCommand_WithSpecificEntry_RemovesIt()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var rule = new MutableMapRemoteRule(priority: 350, isEnabled: true);
            using var vm = new MapRemoteViewModel(rule, InlineUserInterfaceScheduler.Instance);
            vm.NewPatternText = "*.example.com";
            vm.DestinationHost = "a.example.com";
            vm.AddEntryCommand.Execute(null);
            vm.NewPatternText = "*.example.org";
            vm.DestinationHost = "b.example.org";
            vm.AddEntryCommand.Execute(null);
            await Assert.That(vm.Entries.Count).IsEqualTo(2);

            var first = vm.Entries.First(e => e.Entry.MatchingRule.Pattern == "*.example.com");
            vm.RemoveEntryCommand.Execute(first);

            await Assert.That(vm.Entries.Count).IsEqualTo(1);
            await Assert.That(vm.Entries[0].Entry.MatchingRule.Pattern).IsEqualTo("*.example.org");
        });
    }
}
