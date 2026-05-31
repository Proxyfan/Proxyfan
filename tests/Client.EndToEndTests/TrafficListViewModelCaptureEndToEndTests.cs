using Proxyfan.Client.EndToEndTests.Fixtures;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering <see cref="Proxyfan.Client.Traffic.ViewModels.TrafficListViewModel" />'s
///     traffic-capture surface as described in <c>docs/DESIGN.md § 6.1</c>:
///     <list type="bullet">
///         <item>capture toggle button (Pause / Resume),</item>
///         <item>empty-list state on a freshly created shell,</item>
///         <item>flows appearing as <c>LoadFlows(...)</c> is invoked,</item>
///         <item>clear command resets the list,</item>
///         <item>capture-paused indicator in the status bar.</item>
///     </list>
/// </summary>
public sealed class TrafficListViewModelCaptureEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task IsCapturing_FreshShell_StartsCapturing()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            await Assert.That(env.ShellViewModel.TrafficList.IsCapturing).IsTrue();
        });
    }

    [Test]
    public async Task ToggleCapture_FromCapturing_TransitionsToPaused()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;

            vm.ToggleCaptureCommand.Execute(null);

            await Assert.That(vm.IsCapturing).IsFalse();
        });
    }

    [Test]
    public async Task ToggleCapture_TwiceFromCapturing_ReturnsToCapturing()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;

            vm.ToggleCaptureCommand.Execute(null);
            vm.ToggleCaptureCommand.Execute(null);

            await Assert.That(vm.IsCapturing).IsTrue();
        });
    }

    [Test]
    public async Task Flows_FreshShell_IsEmpty()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task LoadFlows_TwoFlowsFromHar_AppearInOrder()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;
            var imported = new List<TrafficFlow>
            {
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/one"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/two"),
            };

            vm.LoadFlows(imported);

            await Assert.That(vm.Flows.Count).IsEqualTo(2);
            await Assert.That(vm.Flows[0].Host).IsEqualTo("api.example.com");
            await Assert.That(vm.Flows[0].PathAndQuery).IsEqualTo("/one");
            await Assert.That(vm.Flows[1].PathAndQuery).IsEqualTo("/two");
        });
    }

    [Test]
    public async Task Clear_PopulatedList_RemovesAllFlows()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;
            vm.LoadFlows([EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x")]);

            vm.ClearCommand.Execute(null);

            await Assert.That(vm.Flows.Count).IsEqualTo(0);
            await Assert.That(vm.VisibleFlows.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task LoadFlows_ThenClear_BothCommandsObservable()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;
            vm.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/a"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/b"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(3, "https://api.example.com/c"),
            ]);
            await Assert.That(vm.Flows.Count).IsEqualTo(3);

            vm.ClearCommand.Execute(null);

            await Assert.That(vm.Flows.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task LoadFlows_EmptyList_ProducesEmptyState()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;

            vm.LoadFlows(Array.Empty<TrafficFlow>());

            await Assert.That(vm.Flows.Count).IsEqualTo(0);
            await Assert.That(vm.VisibleFlows.Count).IsEqualTo(0);
        });
    }
}
