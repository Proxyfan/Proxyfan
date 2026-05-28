using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Domain.Traffic.Tabs;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Behaviour tests for <see cref="TabHostViewModel" /> covering the multi-tab UI
///     contract: add, close, navigate, persist filter, persist selection.
/// </summary>
public sealed class TabHostViewModelTests
{
    [Test]
    public async Task Constructor_NewInstance_HasOneTabNamedAllTraffic()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);

        await Assert.That(host.Tabs.Count).IsEqualTo(1);
        await Assert.That(host.Tabs[0].Name).IsEqualTo(TrafficWorkspaceTabSet.DefaultFirstTabName);
        await Assert.That(host.ActiveTab).IsSameReferenceAs(host.Tabs[0]);
        await Assert.That(host.ActiveTabIndex).IsEqualTo(0);
        await Assert.That(host.Tabs[0].IsCloseable).IsFalse();
    }

    [Test]
    public async Task AddTab_FromSingleTab_AppendsSecondTabAndMarksBothCloseable()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);

        host.AddTabCommand.Execute(null);

        await Assert.That(host.Tabs.Count).IsEqualTo(2);
        await Assert.That(host.ActiveTabIndex).IsEqualTo(1);
        await Assert.That(host.ActiveTab.Name).IsEqualTo("Tab 2");
        await Assert.That(host.Tabs[0].IsCloseable).IsTrue();
        await Assert.That(host.Tabs[1].IsCloseable).IsTrue();
    }

    [Test]
    public async Task CloseTab_LastRemainingTab_DoesNothing()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);

        host.CloseTabCommand.Execute(host.ActiveTab);

        await Assert.That(host.Tabs.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CloseActiveTab_AfterAdd_RemovesActiveAndActivatesPrevious()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);
        host.AddTabCommand.Execute(null);
        await Assert.That(host.ActiveTabIndex).IsEqualTo(1);

        host.CloseActiveTabCommand.Execute(null);

        await Assert.That(host.Tabs.Count).IsEqualTo(1);
        await Assert.That(host.ActiveTabIndex).IsEqualTo(0);
        await Assert.That(host.ActiveTab.Name).IsEqualTo(TrafficWorkspaceTabSet.DefaultFirstTabName);
        await Assert.That(host.Tabs[0].IsCloseable).IsFalse();
    }

    [Test]
    public async Task ActivateAt_OutOfRange_IsIgnored()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);

        host.ActivateAt(-1);
        host.ActivateAt(99);

        await Assert.That(host.ActiveTabIndex).IsEqualTo(0);
    }

    [Test]
    public async Task ActivateNext_FromSingleTab_NoChange()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);

        host.ActivateNext();

        await Assert.That(host.ActiveTabIndex).IsEqualTo(0);
    }

    [Test]
    public async Task ActivateNext_WithTwoTabs_WrapsAroundOnSecondCall()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);
        host.AddTabCommand.Execute(null);
        host.ActivateAt(0);

        host.ActivateNext();
        await Assert.That(host.ActiveTabIndex).IsEqualTo(1);

        host.ActivateNext();
        await Assert.That(host.ActiveTabIndex).IsEqualTo(0);
    }

    [Test]
    public async Task ActivatePrevious_WithTwoTabs_WrapsAroundFromFirstToLast()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);
        host.AddTabCommand.Execute(null);
        host.ActivateAt(0);

        host.ActivatePrevious();

        await Assert.That(host.ActiveTabIndex).IsEqualTo(1);
    }

    [Test]
    public async Task ActiveTabSwitch_FilterText_IsPersistedAndRestored()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);
        trafficList.FilterText = "api";

        host.AddTabCommand.Execute(null);
        await Assert.That(trafficList.FilterText).IsEqualTo(string.Empty);

        trafficList.FilterText = "images";

        host.ActivateAt(0);
        await Assert.That(trafficList.FilterText).IsEqualTo("api");

        host.ActivateAt(1);
        await Assert.That(trafficList.FilterText).IsEqualTo("images");
    }

    [Test]
    public async Task ActiveTabSwitch_SelectedFlow_IsPersistedAndRestored()
    {
        var trafficList = BuildTrafficList();
        var flow = AddFlow(trafficList, "host.example", 1);
        var host = new TabHostViewModel(trafficList);
        trafficList.SelectedFlow = flow;

        host.AddTabCommand.Execute(null);
        await Assert.That(trafficList.SelectedFlow).IsNull();

        host.ActivateAt(0);
        await Assert.That(trafficList.SelectedFlow).IsSameReferenceAs(flow);
    }

    [Test]
    public async Task TabViewModel_Rename_UpdatesNameAndIgnoresWhitespace()
    {
        var domainTab = new TrafficWorkspaceTab("Initial");
        var viewModel = new TabViewModel(domainTab);

        viewModel.Rename("   ");
        await Assert.That(viewModel.Name).IsEqualTo("Initial");

        viewModel.Rename(null);
        await Assert.That(viewModel.Name).IsEqualTo("Initial");

        viewModel.Rename("Renamed");
        await Assert.That(viewModel.Name).IsEqualTo("Renamed");
        await Assert.That(domainTab.Name).IsEqualTo("Renamed");
    }

    [Test]
    public async Task TabViewModel_Id_MatchesUnderlyingDomainTab()
    {
        var domainTab = new TrafficWorkspaceTab("X");
        var viewModel = new TabViewModel(domainTab);

        await Assert.That(viewModel.Id).IsEqualTo(domainTab.Id);
        await Assert.That(viewModel.Source).IsSameReferenceAs(domainTab);
    }

    [Test]
    public async Task CloseTab_NullArgument_IsIgnored()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);
        host.AddTabCommand.Execute(null);

        host.CloseTabCommand.Execute(null);

        await Assert.That(host.Tabs.Count).IsEqualTo(2);
    }

    [Test]
    public async Task CloseTab_NonActiveTabAfterActiveIndex_AdjustsCountAndKeepsActive()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);
        host.AddTabCommand.Execute(null);
        host.AddTabCommand.Execute(null);
        host.ActivateAt(0);

        var toClose = host.Tabs[2];
        host.CloseTabCommand.Execute(toClose);

        await Assert.That(host.Tabs.Count).IsEqualTo(2);
        await Assert.That(host.ActiveTabIndex).IsEqualTo(0);
    }

    [Test]
    public async Task FilterChange_OnActiveTab_PropagatesToDomainTab()
    {
        var trafficList = BuildTrafficList();
        var host = new TabHostViewModel(trafficList);

        trafficList.FilterText = "match-this";

        await Assert.That(host.ActiveTab.Source.FilterQuery).IsEqualTo("match-this");
    }

    [Test]
    public async Task SelectedFlowChange_OnActiveTab_PropagatesToDomainTab()
    {
        var trafficList = BuildTrafficList();
        var flow = AddFlow(trafficList, "x.example", 1);
        var host = new TabHostViewModel(trafficList);

        trafficList.SelectedFlow = flow;

        await Assert.That(host.ActiveTab.Source.SelectedFlowId).IsEqualTo(flow.Source.Id);
    }

    private static TrafficFlowViewModel AddFlow(TrafficListViewModel trafficList, string host, int number)
    {
        var uri = new Uri("https://" + host + "/");
        var headers = HeaderCollection.Empty.Add("Host", host);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = uri,
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        var domainEvent = new RequestReceived(Guid.NewGuid(), request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var viewModel = new TrafficFlowViewModel(domainEvent, number);
        trafficList.Flows.Add(viewModel);
        return viewModel;
    }

    private static TrafficListViewModel BuildTrafficList()
    {
        var eventBus = new StubDomainEventBus();
        return new TrafficListViewModel(eventBus, InlineUserInterfaceScheduler.Instance);
    }

    private sealed class StubDomainEventBus : IDomainEventBus
    {
        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            return new StubSubscription();
        }

        private sealed class StubSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
