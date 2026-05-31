using Proxyfan.Client.EndToEndTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering <see cref="Proxyfan.Client.Shell.ViewModels.TabHostViewModel" />
///     tab-strip behaviour from <c>docs/DESIGN.md § 6.25 Multiple Tabs</c>:
///     opening a new tab from the toolbar, closing a tab with the per-tab close
///     button, and the active-tab index following user actions.
/// </summary>
public sealed class TabHostViewModelEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task Tabs_FreshShell_ContainsSingleDefaultTab()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            await Assert.That(env.ShellViewModel.TabHost.Tabs.Count).IsEqualTo(1);
        });
    }

    [Test]
    public async Task AddTabCommand_Invoked_AppendsTab()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var initial = env.ShellViewModel.TabHost.Tabs.Count;

            env.ShellViewModel.TabHost.AddTabCommand.Execute(null);

            await Assert.That(env.ShellViewModel.TabHost.Tabs.Count).IsEqualTo(initial + 1);
        });
    }

    [Test]
    public async Task ActiveTabIndex_AfterAddingTab_PointsToNewTab()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TabHost.AddTabCommand.Execute(null);

            await Assert.That(env.ShellViewModel.TabHost.ActiveTabIndex).IsEqualTo(env.ShellViewModel.TabHost.Tabs.Count - 1);
        });
    }

    [Test]
    public async Task CloseTabCommand_OnNewlyAddedTab_RemovesIt()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TabHost.AddTabCommand.Execute(null);
            var added = env.ShellViewModel.TabHost.Tabs[^1];

            env.ShellViewModel.TabHost.CloseTabCommand.Execute(added);

            await Assert.That(env.ShellViewModel.TabHost.Tabs.Count).IsEqualTo(1);
        });
    }

    [Test]
    public async Task CloseActiveTabCommand_AfterAddingTab_RemovesActiveTab()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TabHost.AddTabCommand.Execute(null);
            var beforeClose = env.ShellViewModel.TabHost.Tabs.Count;

            env.ShellViewModel.TabHost.CloseActiveTabCommand.Execute(null);

            await Assert.That(env.ShellViewModel.TabHost.Tabs.Count).IsEqualTo(beforeClose - 1);
        });
    }
}
