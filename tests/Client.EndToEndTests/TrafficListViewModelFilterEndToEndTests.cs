using Proxyfan.Client.EndToEndTests.Fixtures;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.EndToEndTests.Pages;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering <see cref="Proxyfan.Client.Traffic.ViewModels.TrafficListViewModel" />
///     filter behaviour from <c>docs/DESIGN.md § 6.4</c>: typing into the filter
///     text box narrows the visible flow list to entries whose URL (or other
///     filtered field) matches the typed substring.
/// </summary>
public sealed class TrafficListViewModelFilterEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task FilterText_TypedIntoToolbar_PropagatesToViewModel()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            var filter = page.FilterTextBox();
            filter.Text = "example.com";

            await Assert.That(env.ShellViewModel.TrafficList.FilterText).IsEqualTo("example.com");
        });
    }

    [Test]
    public async Task FilterText_MatchingSubstring_NarrowsVisibleFlows()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;
            vm.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/users"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://cdn.example.com/styles.css"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(3, "https://other.com/index.html"),
            ]);

            vm.FilterText = "example.com";

            await Assert.That(vm.VisibleFlows.Count).IsEqualTo(2);
        });
    }

    [Test]
    public async Task FilterText_NonMatching_ProducesZeroVisibleFlows()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;
            vm.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/users"),
            ]);

            vm.FilterText = "zzz-no-match-zzz";

            await Assert.That(vm.VisibleFlows.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task FilterText_CleartoEmpty_RestoresAllFlowsVisible()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;
            vm.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/a"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/b"),
            ]);
            vm.FilterText = "no-match";
            await Assert.That(vm.VisibleFlows.Count).IsEqualTo(0);

            vm.FilterText = string.Empty;

            await Assert.That(vm.VisibleFlows.Count).IsEqualTo(2);
        });
    }

    [Test]
    public async Task FilterText_WhitespaceOnly_TreatedAsNoFilter()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var vm = env.ShellViewModel.TrafficList;
            vm.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ]);

            vm.FilterText = "   ";

            await Assert.That(vm.VisibleFlows.Count).IsEqualTo(1);
        });
    }
}
