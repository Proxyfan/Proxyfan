using Avalonia.Controls;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.EndToEndTests.Pages;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering <see cref="Proxyfan.Client.Shell.Views.ShellWindow" />'s
///     application-layout requirements as described in <c>docs/DESIGN.md § 4</c>:
///     the main window, three-panel split (Source list | Traffic list | Inspector),
///     menu, toolbar with capture/clear/session/filter controls, and the
///     bottom status bar.
/// </summary>
public sealed class ShellWindowLayoutEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task Show_FreshShell_TitleIsProductName()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            await Assert.That(page.GetTitle()).IsEqualTo("Proxyfan");
        });
    }

    [Test]
    public async Task Show_FreshShell_MenuExposesFileToolsAndViewMenus()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            var headers = page.TopLevelMenuItems()
                              .Select(item => item.Header?.ToString())
                              .ToArray();

            await Assert.That(headers).Contains("File");
            await Assert.That(headers).Contains("Tools");
            await Assert.That(headers).Contains("View");
        });
    }

    [Test]
    public async Task Show_FreshShell_SourceListContainsAllGroup()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            var sourceList = page.SourceList();

            // The source list always has at least the synthetic "All" group with zero flows.
            await Assert.That(sourceList.ItemCount).IsGreaterThanOrEqualTo(1);
        });
    }

    [Test]
    public async Task Show_FreshShell_TabListContainsExactlyOneDefaultTab()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            var tabs = page.TabList();

            await Assert.That(tabs.ItemCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task Show_FreshShell_FilterTextBoxIsEmpty()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            var filter = page.FilterTextBox();

            await Assert.That(filter.Text ?? string.Empty).IsEqualTo(string.Empty);
        });
    }

    [Test]
    public async Task Show_FreshShell_VisualTreeContainsExpectedNumberOfGridSplitters()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var splitters = UiTreeFinder.FindAll<GridSplitter>(env.Window);

            // ShellView.axaml declares two splitters between the three central panels
            // (source | traffic | inspector).
            await Assert.That(splitters.Count).IsEqualTo(2);
        });
    }
}
