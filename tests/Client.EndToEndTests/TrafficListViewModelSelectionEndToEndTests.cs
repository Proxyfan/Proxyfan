using Proxyfan.Client.EndToEndTests.Fixtures;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering <see cref="Proxyfan.Client.Traffic.ViewModels.TrafficListViewModel" />
///     selection state and the <c>RemoveSelected</c> command. Verifies the
///     selection-driven inspector contract from <c>docs/DESIGN.md § 6.3
///     Traffic Inspection</c> and the explicit delete-flow path documented in
///     § 6.1 Traffic Capture (Delete key).
/// </summary>
public sealed class TrafficListViewModelSelectionEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task SelectedFlow_FreshShell_IsNull()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            await Assert.That(env.ShellViewModel.TrafficList.SelectedFlow).IsNull();
        });
    }

    [Test]
    public async Task SelectedFlow_SetToLoadedFlow_BecomesSelected()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/a"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/b"),
            ]);
            var firstFlow = env.ShellViewModel.TrafficList.Flows[0];

            env.ShellViewModel.TrafficList.SelectedFlow = firstFlow;

            await Assert.That(env.ShellViewModel.TrafficList.SelectedFlow).IsSameReferenceAs(firstFlow);
        });
    }

    [Test]
    public async Task RemoveSelected_NoSelection_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ]);
            env.ShellViewModel.TrafficList.SelectedFlow = null;

            env.ShellViewModel.TrafficList.RemoveSelectedCommand.Execute(null);

            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(1);
        });
    }

    [Test]
    public async Task RemoveSelected_WithSelection_RemovesAndClearsSelection()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/a"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/b"),
            ]);
            env.ShellViewModel.TrafficList.SelectedFlow = env.ShellViewModel.TrafficList.Flows[0];

            env.ShellViewModel.TrafficList.RemoveSelectedCommand.Execute(null);

            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(1);
            await Assert.That(env.ShellViewModel.TrafficList.SelectedFlow).IsNull();
        });
    }

    [Test]
    public async Task RemoveSelected_AfterClear_DoesNotResurrectAnything()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ]);
            env.ShellViewModel.TrafficList.SelectedFlow = env.ShellViewModel.TrafficList.Flows[0];
            env.ShellViewModel.TrafficList.ClearCommand.Execute(null);

            env.ShellViewModel.TrafficList.RemoveSelectedCommand.Execute(null);

            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(0);
            await Assert.That(env.ShellViewModel.TrafficList.SelectedFlow).IsNull();
        });
    }
}
