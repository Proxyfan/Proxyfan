using Proxyfan.Client.EndToEndTests.Fixtures;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.Traffic.ViewModels;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the <see cref="SourceListViewModel" />
///     selection-narrows-traffic interaction from <c>docs/DESIGN.md § 4.2 Source
///     List Panel</c>. Selecting a host group sets the underlying
///     <see cref="TrafficListViewModel.HostFilter" /> and narrows the visible
///     flows accordingly; selecting the synthetic "All" group clears it.
/// </summary>
public sealed class SourceListViewModelEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task Sources_FreshShell_ContainsExactlyTheAllGroup()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            await Assert.That(env.ShellViewModel.SourceList.Sources.Count).IsEqualTo(1);
            await Assert.That(env.ShellViewModel.SourceList.Sources[0].IsAllGroup).IsTrue();
        });
    }

    [Test]
    public async Task SelectedSource_FreshShell_IsAllGroup()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            await Assert.That(env.ShellViewModel.SourceList.SelectedSource).IsNotNull();
            await Assert.That(env.ShellViewModel.SourceList.SelectedSource!.IsAllGroup).IsTrue();
        });
    }

    [Test]
    public async Task SelectedSource_SetToHostGroup_PropagatesToHostFilter()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var hostGroup = new SourceGroupViewModel("api.example.com", isAllGroup: false);
            env.ShellViewModel.SourceList.Sources.Add(hostGroup);

            env.ShellViewModel.SourceList.SelectedSource = hostGroup;

            await Assert.That(env.ShellViewModel.TrafficList.HostFilter).IsEqualTo("api.example.com");
        });
    }

    [Test]
    public async Task SelectedSource_SetBackToAllGroup_ClearsHostFilter()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var hostGroup = new SourceGroupViewModel("api.example.com", isAllGroup: false);
            env.ShellViewModel.SourceList.Sources.Add(hostGroup);
            env.ShellViewModel.SourceList.SelectedSource = hostGroup;
            await Assert.That(env.ShellViewModel.TrafficList.HostFilter).IsEqualTo("api.example.com");

            env.ShellViewModel.SourceList.SelectedSource = env.ShellViewModel.SourceList.Sources[0];

            await Assert.That(env.ShellViewModel.TrafficList.HostFilter).IsEqualTo(string.Empty);
        });
    }

    [Test]
    public async Task HostFilter_MatchingHost_NarrowsVisibleFlowsToThatHost()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://cdn.example.com/y"),
            ]);
            await Assert.That(env.ShellViewModel.TrafficList.VisibleFlows.Count).IsEqualTo(2);

            env.ShellViewModel.TrafficList.HostFilter = "api.example.com";

            await Assert.That(env.ShellViewModel.TrafficList.VisibleFlows.Count).IsEqualTo(1);
            await Assert.That(env.ShellViewModel.TrafficList.VisibleFlows[0].Host).IsEqualTo("api.example.com");
        });
    }
}
