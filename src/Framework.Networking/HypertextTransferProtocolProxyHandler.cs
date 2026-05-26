using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Handles plain HTTP/1.1 proxy requests by forwarding them to the upstream origin,
///     capturing request and response data, and storing completed traffic flows.
/// </summary>
public sealed class HypertextTransferProtocolProxyHandler : IConnectionHandler
{
    private const int DefaultHypertextTransferProtocolPort = 80;
    private const int MaxHeaderBytes = 65536;
    private static readonly byte[][] MethodPrefixes;
    private readonly IDomainEventBus _eventBus;
    private readonly ILogger<HypertextTransferProtocolProxyHandler> _logger;
    private readonly ITrafficStore _trafficStore;

    static HypertextTransferProtocolProxyHandler()
    {
        var methodPrefixes = new byte[][]
        {
            Encoding.ASCII.GetBytes("DELETE "),
            Encoding.ASCII.GetBytes("GET "),
            Encoding.ASCII.GetBytes("HEAD "),
            Encoding.ASCII.GetBytes("OPTIONS "),
            Encoding.ASCII.GetBytes("PATCH "),
            Encoding.ASCII.GetBytes("POST "),
            Encoding.ASCII.GetBytes("PUT "),
            Encoding.ASCII.GetBytes("TRACE "),
        };
        MethodPrefixes = methodPrefixes;
    }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolProxyHandler" /> instance.
    /// </summary>
    /// <param name="trafficStore">
    ///     The store that persists captured traffic flows.
    /// </param>
    /// <param name="eventBus">
    ///     The domain event bus used to publish traffic capture events.
    /// </param>
    /// <param name="logger">
    ///     The logger used for structured diagnostic output.
    /// </param>
    public HypertextTransferProtocolProxyHandler(
        ITrafficStore trafficStore,
        IDomainEventBus eventBus,
        ILogger<HypertextTransferProtocolProxyHandler> logger)
    {
        _trafficStore = trafficStore;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanHandle(ReadOnlySequence<byte> initialBytes)
    {
        foreach (var methodPrefix in MethodPrefixes)
        {
            if (CanStartWith(initialBytes, methodPrefix))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task HandleAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var requestExchange = await HypertextTransferProtocolPipeHelpers.ReadRequestAsync(connection.Transport.Input, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);

            if (requestExchange is null)
            {
                return;
            }

            var flow = CreateTrafficFlow(connection);
            PublishFlowCreated(flow);
            flow.SetRequest(requestExchange.Request);
            PublishRequestReceived(flow, requestExchange.Request);
            var responseExchange = await ForwardRequestAsync(requestExchange, cancellationToken).ConfigureAwait(false);

            if (responseExchange is null)
            {
                flow.Fail();
                PublishFlowCompleted(flow);
                return;
            }

            flow.SetResponse(responseExchange.Response);
            PublishResponseReceived(flow, responseExchange.Response);
            flow.Complete();
            await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(connection.Transport.Output, responseExchange, cancellationToken).ConfigureAwait(false);
            _trafficStore.Add(flow);
            PublishFlowCompleted(flow);

            if (!CanKeepClientConnectionAlive(requestExchange.Request, responseExchange.Response))
            {
                return;
            }
        }
    }

    private bool CanKeepClientConnectionAlive(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response)
    {
        if (string.Equals(request.Version, "HTTP/1.0", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!response.Headers.HasHeader("Content-Length"))
        {
            return false;
        }

        if (HasConnectionCloseDirective(request.Headers) || HasConnectionCloseDirective(response.Headers))
        {
            return false;
        }

        return true;
    }

    private bool CanStartWith(ReadOnlySequence<byte> initialBytes, byte[] prefix)
    {
        if (initialBytes.Length < prefix.Length)
        {
            return false;
        }

        Span<byte> candidatePrefix = stackalloc byte[prefix.Length];
        initialBytes.Slice(0, prefix.Length).CopyTo(candidatePrefix);
        return candidatePrefix.SequenceEqual(prefix);
    }

    private TrafficFlow CreateTrafficFlow(IProxyConnection connection)
    {
        var clientEndPoint = connection.RemoteEndPoint?.ToString() ?? "unknown";
        var flow = new TrafficFlow(Guid.NewGuid(), clientEndPoint, DateTimeOffset.UtcNow);
        return flow;
    }

    private async Task<HypertextTransferProtocolProxyResponseExchange?> ForwardRequestAsync(
        HypertextTransferProtocolProxyRequestExchange requestExchange,
        CancellationToken cancellationToken)
    {
        var hostEndpoint = ParseHostEndpoint(requestExchange.Request.Headers);

        if (hostEndpoint is null)
        {
            _logger.LogDebug("HTTP request is missing a valid Host header.");
            return null;
        }

        using var upstreamClient = new TcpClient();
        await upstreamClient.ConnectAsync(hostEndpoint.Host, hostEndpoint.Port, cancellationToken).ConfigureAwait(false);
        await using var upstreamStream = upstreamClient.GetStream();
        await upstreamStream.WriteAsync(requestExchange.HeaderBytes, cancellationToken).ConfigureAwait(false);
        await upstreamStream.WriteAsync(requestExchange.Body, cancellationToken).ConfigureAwait(false);
        await upstreamStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        var reader = PipeReader.Create(upstreamStream);
        var responseExchange = await HypertextTransferProtocolPipeHelpers.ReadResponseAsync(reader, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);
        await reader.CompleteAsync().ConfigureAwait(false);
        return responseExchange;
    }

    private bool HasConnectionCloseDirective(HeaderCollection headers)
    {
        var connectionValue = headers.Get("Connection");

        if (string.IsNullOrWhiteSpace(connectionValue))
        {
            return false;
        }

        return connectionValue.Contains("close", StringComparison.OrdinalIgnoreCase);
    }

    private ConnectTarget? ParseHostEndpoint(HeaderCollection headers)
    {
        var hostValue = headers.Get("Host");

        if (string.IsNullOrWhiteSpace(hostValue))
        {
            return null;
        }

        var separatorIndex = hostValue.LastIndexOf(':');

        if (separatorIndex < 0)
        {
            var hostWithoutPort = hostValue.Trim();

            if (string.IsNullOrWhiteSpace(hostWithoutPort))
            {
                return null;
            }

            var defaultTarget = new ConnectTarget(hostWithoutPort, DefaultHypertextTransferProtocolPort);
            return defaultTarget;
        }

        var host = hostValue[..separatorIndex].Trim();
        var portText = hostValue[(separatorIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(host) || !int.TryParse(portText, out var port) || port is < 1 or > 65535)
        {
            return null;
        }

        var target = new ConnectTarget(host, port);
        return target;
    }

    private void PublishFlowCompleted(TrafficFlow flow)
    {
        var completedEvent = new TrafficFlowCompleted(flow.Id, flow.Status, DateTimeOffset.UtcNow);
        _eventBus.Publish(completedEvent);
    }

    private void PublishFlowCreated(TrafficFlow flow)
    {
        var createdEvent = new TrafficFlowCreated(flow.Id, DateTimeOffset.UtcNow);
        _eventBus.Publish(createdEvent);
    }

    private void PublishRequestReceived(TrafficFlow flow, HypertextTransferProtocolRequestData request)
    {
        var requestReceivedEvent = new RequestReceived(flow.Id, request, flow.ClientEndPoint, DateTimeOffset.UtcNow);
        _eventBus.Publish(requestReceivedEvent);
    }

    private void PublishResponseReceived(TrafficFlow flow, HypertextTransferProtocolResponseData response)
    {
        var responseReceivedEvent = new ResponseReceived(flow.Id, response, DateTimeOffset.UtcNow);
        _eventBus.Publish(responseReceivedEvent);
    }
}