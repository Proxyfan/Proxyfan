using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for the color tag and comment annotation commands on
///     <see cref="TrafficListViewModel" />. Verifies the commands forward to the
///     selected flow's view model and that the underlying domain flow is updated.
/// </summary>
public sealed class TrafficListViewModelAnnotationTests
{
    /// <summary>
    ///     ApplyColorTagToSelected sets both the view model and the underlying domain flow.
    /// </summary>
    [Test]
    public async Task ApplyColorTagToSelectedCommand_WithSelection_UpdatesFlow()
    {
        var bus = new RecordingBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent());
        viewModel.SelectedFlow = viewModel.Flows[0];

        viewModel.ApplyColorTagToSelectedCommand.Execute(TrafficFlowColorTag.Blue);

        await Assert.That(viewModel.SelectedFlow.ColorTag).IsEqualTo(TrafficFlowColorTag.Blue);
        var domainFlow = viewModel.GetDomainFlow(viewModel.SelectedFlow.Id);
        await Assert.That(domainFlow?.ColorTag).IsEqualTo(TrafficFlowColorTag.Blue);
    }

    /// <summary>
    ///     ApplyColorTagToSelected with no selection does not throw.
    /// </summary>
    [Test]
    public async Task ApplyColorTagToSelectedCommand_NoSelection_DoesNothing()
    {
        var bus = new RecordingBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);

        await Assert.That(() => viewModel.ApplyColorTagToSelectedCommand.Execute(TrafficFlowColorTag.Red)).ThrowsNothing();
    }

    /// <summary>
    ///     ApplyCommentToSelected sets both the view model and the underlying domain flow.
    /// </summary>
    [Test]
    public async Task ApplyCommentToSelectedCommand_WithSelection_UpdatesFlow()
    {
        var bus = new RecordingBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent());
        viewModel.SelectedFlow = viewModel.Flows[0];

        viewModel.ApplyCommentToSelectedCommand.Execute("login failure repro");

        await Assert.That(viewModel.SelectedFlow.Comment).IsEqualTo("login failure repro");
        var domainFlow = viewModel.GetDomainFlow(viewModel.SelectedFlow.Id);
        await Assert.That(domainFlow?.Comment).IsEqualTo("login failure repro");
    }

    /// <summary>
    ///     ApplyCommentToSelected with null clears any existing comment.
    /// </summary>
    [Test]
    public async Task ApplyCommentToSelectedCommand_WithNull_ClearsComment()
    {
        var bus = new RecordingBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent());
        viewModel.SelectedFlow = viewModel.Flows[0];
        viewModel.SelectedFlow.ApplyComment("prior");

        viewModel.ApplyCommentToSelectedCommand.Execute(null);

        await Assert.That(viewModel.SelectedFlow.Comment).IsNull();
        var domainFlow = viewModel.GetDomainFlow(viewModel.SelectedFlow.Id);
        await Assert.That(domainFlow?.Comment).IsNull();
    }

    /// <summary>
    ///     ApplyCommentToSelected with no selection does not throw.
    /// </summary>
    [Test]
    public async Task ApplyCommentToSelectedCommand_NoSelection_DoesNothing()
    {
        var bus = new RecordingBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);

        await Assert.That(() => viewModel.ApplyCommentToSelectedCommand.Execute("anything")).ThrowsNothing();
    }

    private static RequestReceived CreateRequestEvent()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/api"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return new RequestReceived(Guid.NewGuid(), request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
    }

    private sealed class RecordingBus : IDomainEventBus
    {
        private readonly List<DomainEventHandler<RequestReceived>> _requestHandlers = [];

        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            if (handler is DomainEventHandler<RequestReceived> requestHandler)
            {
                _requestHandlers.Add(requestHandler);
            }

            return new NoOpSubscription();
        }

        public void PublishRequestReceived(RequestReceived domainEvent)
        {
            foreach (var handler in _requestHandlers)
            {
                handler(domainEvent);
            }
        }

        private sealed class NoOpSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
