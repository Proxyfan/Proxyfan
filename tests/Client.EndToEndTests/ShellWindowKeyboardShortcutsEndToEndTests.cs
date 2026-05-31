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
///     End-to-end UI tests covering <c>docs/DESIGN.md § 9 Keyboard Shortcuts</c>
///     for the global gestures declared on <see cref="Proxyfan.Client.Shell.Views.ShellWindow" />.
///     The headless platform routes simulated key gestures through Avalonia's
///     real input pipeline, so KeyBinding handlers, focus management and command
///     CanExecute checks are all exercised exactly as on a real desktop session.
/// </summary>
public sealed class ShellWindowKeyboardShortcutsEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task CtrlR_PressedWhileCapturing_TogglesCaptureToPaused()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            await Assert.That(env.ShellViewModel.TrafficList.IsCapturing).IsTrue();

            page.PressKey(PhysicalKey.R, RawInputModifiers.Control);

            await Assert.That(env.ShellViewModel.TrafficList.IsCapturing).IsFalse();
        });
    }

    [Test]
    public async Task CtrlR_PressedTwice_ReturnsToCapturing()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            page.PressKey(PhysicalKey.R, RawInputModifiers.Control);
            page.PressKey(PhysicalKey.R, RawInputModifiers.Control);

            await Assert.That(env.ShellViewModel.TrafficList.IsCapturing).IsTrue();
        });
    }

    [Test]
    public async Task CtrlK_PressedWithFlowsLoaded_ClearsTheList()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/a"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/b"),
            ]);
            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(2);

            page.PressKey(PhysicalKey.K, RawInputModifiers.Control);

            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task CtrlT_Pressed_AddsAdditionalWorkspaceTab()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            var initialTabs = env.ShellViewModel.TabHost.Tabs.Count;

            page.PressKey(PhysicalKey.T, RawInputModifiers.Control);

            await Assert.That(env.ShellViewModel.TabHost.Tabs.Count).IsEqualTo(initialTabs + 1);
        });
    }

    [Test]
    public async Task CtrlShiftN_Pressed_TogglesNoCachingState()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            var before = env.ShellViewModel.IsNoCachingEnabled;

            page.PressKey(PhysicalKey.N, RawInputModifiers.Control | RawInputModifiers.Shift);

            await Assert.That(env.ShellViewModel.IsNoCachingEnabled).IsEqualTo(!before);
        });
    }

    [Test]
    public async Task CtrlShiftB_Pressed_TogglesBreakpointState()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            var before = env.ShellViewModel.IsBreakpointEnabled;

            page.PressKey(PhysicalKey.B, RawInputModifiers.Control | RawInputModifiers.Shift);

            await Assert.That(env.ShellViewModel.IsBreakpointEnabled).IsEqualTo(!before);
        });
    }
}
