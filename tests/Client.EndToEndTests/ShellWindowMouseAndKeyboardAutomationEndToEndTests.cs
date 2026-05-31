using Avalonia.Headless;
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
///     True UI-automation tests that drive <see cref="Proxyfan.Client.Shell.Views.ShellWindow" />
///     through Avalonia's headless input pipeline — every interaction is a mouse
///     event or a keyboard event routed through the framework exactly as a real
///     user would trigger it. Asserts on the observable application state that
///     results from each gesture (rather than on the gesture being delivered).
///     <para>
///         Complements the existing wave-1..5 tests that exercise commands
///         directly; together they give defence-in-depth across
///         <c>docs/DESIGN.md § 4 Application Layout</c>, § 6.1 Traffic Capture,
///         and § 6.4 Traffic Filtering.
///     </para>
/// </summary>
public sealed class ShellWindowMouseAndKeyboardAutomationEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task ClickPauseButton_WhileCapturing_TogglesCaptureOff()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            await Assert.That(env.ShellViewModel.TrafficList.IsCapturing).IsTrue();

            var pause = page.ToolbarButton("Pause Capture");
            page.Click(pause);

            await Assert.That(env.ShellViewModel.TrafficList.IsCapturing).IsFalse();
        });
    }

    [Test]
    public async Task ClickResumeButton_WhilePaused_TogglesCaptureOn()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            env.ShellViewModel.TrafficList.IsCapturing = false;
            page.PumpUiJobs();

            var resume = page.ToolbarButton("Resume Capture");
            page.Click(resume);

            await Assert.That(env.ShellViewModel.TrafficList.IsCapturing).IsTrue();
        });
    }

    [Test]
    public async Task ClickClearButton_WithLoadedFlows_EmptiesTheList()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/y"),
            ]);
            page.PumpUiJobs();

            var clear = page.ToolbarButton("Clear");
            page.Click(clear);

            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task ClickFilterTextBox_ThenTypeText_PropagatesToVisibleFlows()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://cdn.example.com/y"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(3, "https://other.com/z"),
            ]);
            page.PumpUiJobs();

            var filter = page.FilterTextBox();
            page.Click(filter);
            page.TypeText("example.com");
            page.PumpUiJobs();

            await Assert.That(env.ShellViewModel.TrafficList.FilterText).IsEqualTo("example.com");
            await Assert.That(env.ShellViewModel.TrafficList.VisibleFlows.Count).IsEqualTo(2);
        });
    }

    [Test]
    public async Task ClickAddTabButton_FreshShell_AppendsWorkspaceTab()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            var initial = env.ShellViewModel.TabHost.Tabs.Count;
            page.PumpUiJobs();

            var addButton = UiTreeFinder.FindByAutomationName<Avalonia.Controls.Button>(env.Window, "New tab");
            page.Click(addButton);

            await Assert.That(env.ShellViewModel.TabHost.Tabs.Count).IsEqualTo(initial + 1);
        });
    }

    [Test]
    public async Task ClickEnableSystemProxyButton_FromDisabled_RegistersProxyAndSwapsButton()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment(port: 8123);
            var page = new ShellPage(env.Window);
            await Assert.That(env.ShellViewModel.IsSystemProxyEnabled).IsFalse();
            page.PumpUiJobs();

            var enable = page.ToolbarButton("Enable Proxy");
            page.Click(enable);
            // The async ToggleSystemProxyAsync command needs to complete; pump UI jobs lets it.
            page.PumpUiJobs();
            // The state flip may post UI updates we need to drain.
            for (var i = 0; i < 5 && !env.ShellViewModel.IsSystemProxyEnabled; i++)
            {
                page.PumpUiJobs();
                await Task.Yield();
            }

            await Assert.That(env.ShellViewModel.IsSystemProxyEnabled).IsTrue();
            await Assert.That(env.SystemProxy.RegisteredPorts.Count).IsEqualTo(1);
            await Assert.That(env.SystemProxy.RegisteredPorts[0]).IsEqualTo(8123);
        });
    }

    [Test]
    public async Task KeyboardCtrlR_RoutedThroughInputPipeline_TogglesCapture()
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
    public async Task TypeIntoFilter_ThenBackspace_ShortensTheString()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);
            page.PumpUiJobs();

            var filter = page.FilterTextBox();
            page.Click(filter);
            page.TypeText("abc");
            page.PumpUiJobs();
            await Assert.That(filter.Text).IsEqualTo("abc");

            // Backspace through the real input pipeline removes the trailing character.
            page.Window.KeyPressQwerty(PhysicalKey.Backspace, RawInputModifiers.None);
            page.Window.KeyReleaseQwerty(PhysicalKey.Backspace, RawInputModifiers.None);
            page.PumpUiJobs();

            await Assert.That(filter.Text).IsEqualTo("ab");
        });
    }
}
