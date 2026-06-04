using Avalonia.Controls;
using Proxyfan.Client.EndToEndTests.Fixtures;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.EndToEndTests.Pages;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the status bar (<c>docs/DESIGN.md § 4.7</c>):
///     the flow counter and the "Capture paused" indicator update reactively
///     when the underlying view-model state changes.
/// </summary>
public sealed class ShellWindowStatusBarEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task FlowCount_FreshShell_RendersZero()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var statusBarTexts = StatusBarTextBlockContents(env.Window);

            await Assert.That(statusBarTexts).Contains("0");
        });
    }

    [Test]
    public async Task FlowCount_AfterLoadFlows_ReflectsNewCount()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/y"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(3, "https://api.example.com/z"),
            ]);

            var statusBarTexts = StatusBarTextBlockContents(env.Window);

            await Assert.That(statusBarTexts).Contains("3");
        });
    }

    [Test]
    public async Task CapturePaused_FreshShell_IndicatorNotVisible()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();

            // While capturing, the "Capture paused" indicator's IsVisible is bound
            // to !TrafficList.IsCapturing, so it must NOT appear in the visible status bar.
            var statusBarTexts = StatusBarTextBlockContents(env.Window);

            await Assert.That(statusBarTexts).DoesNotContain("Capture paused");
        });
    }

    [Test]
    public async Task CapturePaused_AfterTogglingOff_IndicatorBecomesVisible()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.ToggleCaptureCommand.Execute(null);

            var statusBarTexts = StatusBarTextBlockContents(env.Window);

            await Assert.That(statusBarTexts).Contains("Capture paused");
        });
    }

    [Test]
    public async Task BreakpointQueue_AfterPauseAdded_ReflectsQueueDepth()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var pause = new BreakpointPause(Guid.NewGuid(), NewRequest("https://example.com/pending"));
            env.BreakpointPauseInbox.Add(pause);

            var statusBarTexts = StatusBarTextBlockContents(env.Window);

            await Assert.That(statusBarTexts).Contains("Breakpoints:");
            await Assert.That(statusBarTexts).Contains("1");
        });
    }

    private static string[] StatusBarTextBlockContents(Avalonia.Visual window)
    {
        return UiTreeFinder.FindAll<TextBlock>(window)
                           .Where(tb => tb.IsVisible)
                           .Select(tb => tb.Text ?? string.Empty)
                           .ToArray();
    }

    private static HypertextTransferProtocolRequestData NewRequest(string url)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", new Uri(url).Host),
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
