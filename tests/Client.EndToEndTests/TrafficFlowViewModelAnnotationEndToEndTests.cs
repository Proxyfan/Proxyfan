using Proxyfan.Client.EndToEndTests.Fixtures;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Domain.Traffic;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the per-flow color tag and comment
///     annotation surface described in <c>docs/DESIGN.md § 6.23 Color Tags and
///     Comments</c> through the traffic list and flow view-model projection.
/// </summary>
public sealed class TrafficFlowViewModelAnnotationEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task ColorTag_FreshFlow_DefaultsToNone()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ]);

            var flowVm = env.ShellViewModel.TrafficList.Flows[0];

            await Assert.That(flowVm.ColorTag).IsEqualTo(TrafficFlowColorTag.None);
        });
    }

    [Test]
    public async Task SetColorTag_OnTrafficFlowViewModel_PropagatesToSource()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ]);
            var flowVm = env.ShellViewModel.TrafficList.Flows[0];

            flowVm.ColorTag = TrafficFlowColorTag.Red;

            await Assert.That(flowVm.ColorTag).IsEqualTo(TrafficFlowColorTag.Red);
        });
    }

    [Test]
    public async Task SetColorTag_ChangedSeveralTimes_FinalValueWins()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ]);
            var flowVm = env.ShellViewModel.TrafficList.Flows[0];

            flowVm.ColorTag = TrafficFlowColorTag.Red;
            flowVm.ColorTag = TrafficFlowColorTag.Green;
            flowVm.ColorTag = TrafficFlowColorTag.None;

            await Assert.That(flowVm.ColorTag).IsEqualTo(TrafficFlowColorTag.None);
        });
    }

    [Test]
    public async Task Comment_FreshFlow_IsNull()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ]);

            var flowVm = env.ShellViewModel.TrafficList.Flows[0];

            await Assert.That(flowVm.Comment).IsNull();
        });
    }

    [Test]
    public async Task Comment_SetToText_BecomesObservable()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ]);
            var flowVm = env.ShellViewModel.TrafficList.Flows[0];

            flowVm.Comment = "Investigating 500 here";

            await Assert.That(flowVm.Comment).IsEqualTo("Investigating 500 here");
        });
    }

    [Test]
    public async Task Comment_SetThenCleared_ReturnsToNull()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ]);
            var flowVm = env.ShellViewModel.TrafficList.Flows[0];

            flowVm.Comment = "temporary note";
            flowVm.Comment = null;

            await Assert.That(flowVm.Comment).IsNull();
        });
    }
}
