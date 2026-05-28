using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Orchestrates a Server-Sent Events (SSE) streaming response by writing the response
///     headers to the client, then relaying body bytes from the upstream server to the client
///     verbatim while a <see cref="ServerSentEventsRelay" /> parses each event into the
///     <see cref="ServerSentEventsFlow" />. The handler runs until the upstream stream closes
///     or cancellation is requested, after which the underlying
///     <see cref="TrafficFlow" /> is marked complete and added to the traffic store.
/// </summary>
public sealed class ServerSentEventsStreamHandler
{
    private readonly IDomainEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly IServerSentEventsStore? _serverSentEventsStore;
    private readonly TimeProvider _timeProvider;
    private readonly ITrafficStore _trafficStore;

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEventsStreamHandler" />.
    /// </summary>
    /// <param name="eventBus">The domain event bus used to publish flow events.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    /// <param name="timeProvider">The time source used for SSE timestamps.</param>
    /// <param name="trafficStore">The traffic store that retains completed flows.</param>
    /// <param name="serverSentEventsStore">The optional store that retains captured SSE flows.</param>
    public ServerSentEventsStreamHandler(
        IDomainEventBus eventBus,
        ILogger logger,
        TimeProvider timeProvider,
        ITrafficStore trafficStore,
        IServerSentEventsStore? serverSentEventsStore)
    {
        _eventBus = eventBus;
        _logger = logger;
        _timeProvider = timeProvider;
        _trafficStore = trafficStore;
        _serverSentEventsStore = serverSentEventsStore;
    }

    /// <summary>
    ///     Writes the response headers to the client and runs the SSE relay until the upstream
    ///     closes the stream.
    /// </summary>
    /// <param name="request">The streaming request bundle.</param>
    /// <param name="cancellationToken">A token that cancels the relay.</param>
    /// <returns>A task that completes when the SSE stream terminates.</returns>
    public async Task HandleAsync(
        ServerSentEventsStreamRequest request,
        CancellationToken cancellationToken)
    {
        var flow = request.Flow;
        var rewrittenResponse = ForwardedResponseRewriter.Rewrite(request.ResponseHeaders);
        var rewrittenExchange = HypertextTransferProtocolRuleApplicator.BuildLocalResponseExchange(rewrittenResponse);

        flow.SetResponse(rewrittenResponse);
        PublishResponseReceived(flow, rewrittenResponse);

        var clientWriter = request.Connection.Transport.Output;
        await clientWriter.WriteAsync(rewrittenExchange.HeaderBytes, cancellationToken).ConfigureAwait(false);
        await clientWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

        var serverSentEventsFlow = new ServerSentEventsFlow(flow);
        _serverSentEventsStore?.Add(serverSentEventsFlow);

        var clientWriteStream = clientWriter.AsStream();
        var upstreamReadStream = ServerSentEventsUpstreamStreams.Resolve(request);

        var relay = new ServerSentEventsRelay(serverSentEventsFlow.RecordEvent, _timeProvider);
        var capturedCount = 0;

        try
        {
            capturedCount = await relay.RelayAsync(upstreamReadStream, clientWriteStream, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ioException)
        {
            _logger.LogDebug(ioException, "Server-Sent Events relay terminated due to I/O error.");
        }
        finally
        {
            serverSentEventsFlow.MarkClosed(_timeProvider.GetUtcNow());
            flow.Complete();
            _trafficStore.Add(flow);
            PublishFlowCompleted(flow);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Captured {EventCount} Server-Sent Events for flow {FlowId}.", capturedCount, flow.Id);
            }
        }
    }

    private void PublishFlowCompleted(TrafficFlow flow)
    {
        var completedEvent = new TrafficFlowCompleted(flow.Id, flow.Status, DateTimeOffset.UtcNow);
        _eventBus.Publish(completedEvent);
    }

    private void PublishResponseReceived(TrafficFlow flow, HypertextTransferProtocolResponseData response)
    {
        var responseReceivedEvent = new ResponseReceived(flow.Id, response, DateTimeOffset.UtcNow);
        _eventBus.Publish(responseReceivedEvent);
    }
}
