using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="Client.Traffic.ViewModels.TrafficFlowViewModel" />.
/// </summary>
public sealed class TrafficFlowViewModelTests
{
    /// <summary>
    ///     Verifies that constructing from a <see cref="RequestReceived" /> event populates immutable properties correctly.
    /// </summary>
    [Test]
    public async Task Constructor_FromRequestReceivedEvent_PopulatesImmutableProperties()
    {
        var requestEvent = CreateRequestEvent();

        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(requestEvent, 1);

        await Assert.That(viewModel.Id).IsEqualTo(requestEvent.TrafficFlowId);
        await Assert.That(viewModel.Method).IsEqualTo("GET");
        await Assert.That(viewModel.Number).IsEqualTo(1);
        await Assert.That(viewModel.ClientEndPoint).IsEqualTo("127.0.0.1:9000");
        await Assert.That(viewModel.StartedAt).IsEqualTo(requestEvent.Timestamp);
    }

    /// <summary>
    ///     Verifies that constructing from a <see cref="RequestReceived" /> event sets initial observable state.
    /// </summary>
    [Test]
    public async Task Constructor_FromRequestReceivedEvent_SetsInitialObservableState()
    {
        var requestEvent = CreateRequestEvent();

        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(requestEvent, 1);

        await Assert.That(viewModel.FlowStatus).IsEqualTo(TrafficFlowStatus.Active);
        await Assert.That(viewModel.StatusCode).IsEqualTo(0);
        await Assert.That(viewModel.Response).IsNull();
        await Assert.That(viewModel.Duration).IsNull();
    }

    /// <summary>
    ///     Verifies that constructing from a completed <see cref="TrafficFlow" /> populates all properties.
    /// </summary>
    [Test]
    public async Task Constructor_FromTrafficFlow_PopulatesAllProperties()
    {
        var flow = CreateCompletedFlow();

        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(flow, 5);

        await Assert.That(viewModel.Id).IsEqualTo(flow.Id);
        await Assert.That(viewModel.Number).IsEqualTo(5);
        await Assert.That(viewModel.FlowStatus).IsEqualTo(TrafficFlowStatus.Complete);
        await Assert.That(viewModel.StatusCode).IsEqualTo(200);
        await Assert.That(viewModel.Response).IsNotNull();
    }

    /// <summary>
    ///     Verifies that <see cref="Client.Traffic.ViewModels.TrafficFlowViewModel.UpdateResponse" /> sets observable response fields.
    /// </summary>
    [Test]
    public async Task UpdateResponse_WithResponseEvent_SetsResponseFields()
    {
        var requestEvent = CreateRequestEvent();
        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(requestEvent, 1);
        var responseEvent = CreateResponseEvent(requestEvent.TrafficFlowId);

        viewModel.UpdateResponse(responseEvent);

        await Assert.That(viewModel.StatusCode).IsEqualTo(200);
        await Assert.That(viewModel.Response).IsNotNull();
        await Assert.That(viewModel.BodySize).IsGreaterThan(0L);
    }

    /// <summary>
    ///     Verifies that <see cref="Client.Traffic.ViewModels.TrafficFlowViewModel.UpdateResponse" />
    ///     raises <see cref="Client.Traffic.ViewModels.TrafficFlowViewModel.Source" /> change notifications
    ///     so custom-column bindings re-evaluate when response data arrives.
    /// </summary>
    [Test]
    public async Task UpdateResponse_WithResponseEvent_RaisesSourcePropertyChanged()
    {
        var requestEvent = CreateRequestEvent();
        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(requestEvent, 1);
        var responseEvent = CreateResponseEvent(requestEvent.TrafficFlowId);
        var sourcePropertyChangedRaised = false;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Client.Traffic.ViewModels.TrafficFlowViewModel.Source))
            {
                sourcePropertyChangedRaised = true;
            }
        };

        viewModel.UpdateResponse(responseEvent);

        await Assert.That(sourcePropertyChangedRaised).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="Client.Traffic.ViewModels.TrafficFlowViewModel.UpdateStatus" /> sets the terminal status and duration.
    /// </summary>
    [Test]
    public async Task UpdateStatus_WithCompletedEvent_SetsStatusAndDuration()
    {
        var requestEvent = CreateRequestEvent();
        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(requestEvent, 1);
        var completedEvent = new TrafficFlowCompleted(requestEvent.TrafficFlowId, TrafficFlowStatus.Complete, requestEvent.Timestamp.AddSeconds(2));

        viewModel.UpdateStatus(completedEvent);

        await Assert.That(viewModel.FlowStatus).IsEqualTo(TrafficFlowStatus.Complete);
        await Assert.That(viewModel.Duration).IsNotNull();
    }

    /// <summary>
    ///     A failed status propagates Fail() on the underlying TrafficFlow source.
    /// </summary>
    [Test]
    public async Task UpdateStatus_WithFailedEvent_TransitionsSourceToFailed()
    {
        var requestEvent = CreateRequestEvent();
        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(requestEvent, 1);
        var failedEvent = new TrafficFlowCompleted(requestEvent.TrafficFlowId, TrafficFlowStatus.Failed, requestEvent.Timestamp.AddSeconds(1));

        viewModel.UpdateStatus(failedEvent);

        await Assert.That(viewModel.FlowStatus).IsEqualTo(TrafficFlowStatus.Failed);
        await Assert.That(viewModel.Source.Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>
    ///     An aborted status propagates Abort() on the underlying TrafficFlow source.
    /// </summary>
    [Test]
    public async Task UpdateStatus_WithAbortedEvent_TransitionsSourceToAborted()
    {
        var requestEvent = CreateRequestEvent();
        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(requestEvent, 1);
        var abortedEvent = new TrafficFlowCompleted(requestEvent.TrafficFlowId, TrafficFlowStatus.Aborted, requestEvent.Timestamp.AddSeconds(1));

        viewModel.UpdateStatus(abortedEvent);

        await Assert.That(viewModel.FlowStatus).IsEqualTo(TrafficFlowStatus.Aborted);
        await Assert.That(viewModel.Source.Status).IsEqualTo(TrafficFlowStatus.Aborted);
    }

    /// <summary>
    ///     A Complete status applied when the source is already in a terminal state does not
    ///     attempt to re-complete the source (no exception thrown).
    /// </summary>
    [Test]
    public async Task UpdateStatus_CompleteWhenSourceAlreadyComplete_DoesNotReCompleteSource()
    {
        var requestEvent = CreateRequestEvent();
        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(requestEvent, 1);
        var completedEvent = new TrafficFlowCompleted(requestEvent.TrafficFlowId, TrafficFlowStatus.Complete, requestEvent.Timestamp.AddSeconds(1));
        viewModel.UpdateStatus(completedEvent);

        viewModel.UpdateStatus(completedEvent);

        await Assert.That(viewModel.FlowStatus).IsEqualTo(TrafficFlowStatus.Complete);
        await Assert.That(viewModel.Source.Status).IsEqualTo(TrafficFlowStatus.Complete);
    }

    private RequestReceived CreateRequestEvent()
    {
        var flowId = Guid.NewGuid();
        var uri = new Uri("https://example.com/api/test");
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = uri,
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        var requestEvent = new RequestReceived(flowId, request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
        return requestEvent;
    }

    private ResponseReceived CreateResponseEvent(Guid flowId)
    {
        byte[] body = [1, 2, 3];
        var headers = HeaderCollection.Empty.Add("Content-Length", "3");
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(parameters);
        var responseEvent = new ResponseReceived(flowId, response, DateTimeOffset.UtcNow);
        return responseEvent;
    }

    private TrafficFlow CreateCompletedFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var uri = new Uri("https://example.com/api/test");
        var requestHeaders = HeaderCollection.Empty.Add("Host", "example.com");
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = requestHeaders,
            Method = "GET",
            RequestUri = uri,
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(requestParameters);
        flow.SetRequest(request);
        byte[] responseBody = [1, 2, 3];
        var responseHeaders = HeaderCollection.Empty.Add("Content-Length", "3");
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = responseBody,
            Headers = responseHeaders,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);
        flow.SetResponse(response);
        flow.Complete();
        return flow;
    }
}