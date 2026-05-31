using Avalonia.Input;
using Proxyfan.Client.EndToEndTests.Fixtures;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.EndToEndTests.Pages;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the <c>Delete</c> key binding and
///     <c>Ctrl+W</c> close-tab gesture declared on
///     <see cref="Proxyfan.Client.Shell.Views.ShellWindow" />.
///     Both are part of <c>docs/DESIGN.md § 9 Keyboard Shortcuts</c>.
/// </summary>
public sealed class ShellWindowKeyboardShortcutsExtraEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task Delete_PressedWithSelectedFlow_RemovesIt()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/a"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/b"),
            ]);
            env.ShellViewModel.TrafficList.SelectedFlow = env.ShellViewModel.TrafficList.Flows[0];

            page.PressKey(PhysicalKey.Delete);

            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(1);
        });
    }

    [Test]
    public async Task CtrlW_PressedWithSecondaryTab_ClosesActiveTab()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            // Add a second tab so the active tab is closeable; the default tab is sticky.
            env.ShellViewModel.TabHost.AddTabCommand.Execute(null);
            var beforeClose = env.ShellViewModel.TabHost.Tabs.Count;

            page.PressKey(PhysicalKey.W, RawInputModifiers.Control);

            await Assert.That(env.ShellViewModel.TabHost.Tabs.Count).IsEqualTo(beforeClose - 1);
        });
    }
}
