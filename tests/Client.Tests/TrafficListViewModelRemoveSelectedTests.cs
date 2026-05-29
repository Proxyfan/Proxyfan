using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests covering the <see cref="TrafficListViewModel.RemoveSelectedCommand" /> behavior
///     wired to the Delete keyboard shortcut in the shell window.
/// </summary>
public sealed class TrafficListViewModelRemoveSelectedTests
{
    /// <summary>
    ///     RemoveSelected with no selection must not throw and must leave the collection intact.
    /// </summary>
    [Test]
    public async Task RemoveSelected_NoSelection_NoOps()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var flow = CreateFlow();
        viewModel.Flows.Add(new TrafficFlowViewModel(flow, 1));
        viewModel.SelectedFlow = null;

        viewModel.RemoveSelectedCommand.Execute(parameter: null);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     RemoveSelected with a current selection must remove it from the collection and clear it.
    /// </summary>
    [Test]
    public async Task RemoveSelected_GivenSelection_RemovesAndClears()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var flow = CreateFlow();
        var flowViewModel = new TrafficFlowViewModel(flow, 1);
        viewModel.Flows.Add(flowViewModel);
        viewModel.SelectedFlow = flowViewModel;

        viewModel.RemoveSelectedCommand.Execute(parameter: null);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(0);
        await Assert.That(viewModel.SelectedFlow).IsNull();
    }

    /// <summary>
    ///     RemoveSelected with the selection set to a different view-model than the one being
    ///     removed must not clear the active selection.
    /// </summary>
    [Test]
    public async Task RemoveSelected_SelectionDiffersFromRemoval_KeepsSelection()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var firstFlow = new TrafficFlowViewModel(CreateFlow(), 1);
        var secondFlow = new TrafficFlowViewModel(CreateFlow(), 2);
        viewModel.Flows.Add(firstFlow);
        viewModel.Flows.Add(secondFlow);
        viewModel.SelectedFlow = secondFlow;
        viewModel.SelectedFlow = firstFlow;

        viewModel.RemoveSelectedCommand.Execute(parameter: null);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(1);
        await Assert.That(viewModel.Flows[0]).IsSameReferenceAs(secondFlow);
        await Assert.That(viewModel.SelectedFlow).IsNull();
    }

    private static TrafficFlow CreateFlow()
    {
        return new TrafficFlow(Guid.NewGuid(), "127.0.0.1:0", DateTimeOffset.UtcNow);
    }

    private sealed class StubBus : IDomainEventBus
    {
        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            return new NoopSubscription();
        }

        private sealed class NoopSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
