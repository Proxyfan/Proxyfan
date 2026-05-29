using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolUpgradeOrchestrator" />. The tests stand up
///     a real loopback TCP listener that pretends to be the upstream server, then drives the
///     orchestrator through plain-200, garbage-response, and 101-no-WebSocket paths.
/// </summary>
public sealed class HypertextTransferProtocolUpgradeOrchestratorTests
{
    /// <summary>
    ///     Verifies that a non-101 (plain 200 OK) upstream response is forwarded to the client
    ///     and the flow is completed (no tunnel).
    /// </summary>
    [Test]
    public async Task DispatchAsync_UpstreamReturns200_CompletesFlowAndForwardsResponse()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var responseBytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
        var serverTask = Task.Run(async () =>
        {
            using var acceptedClient = await listener.AcceptTcpClientAsync();
            using var stream = acceptedClient.GetStream();
            await ConsumeRequestHeadersAsync(stream);
            await stream.WriteAsync(responseBytes);
            await stream.FlushAsync();
        });

        try
        {
            var bus = new StubDomainEventBus();
            var trafficStore = new StubTrafficStore();
            var orchestrator = BuildOrchestrator(bus, trafficStore);
            var connection = new StubFullDuplexProxyConnection();
            var upgradeRequest = BuildUpgradeRequest(connection);
            upgradeRequest.Flow.SetRequest(upgradeRequest.EffectiveRequest);
            var target = new ConnectTarget("127.0.0.1", port);

            await orchestrator.DispatchAsync(upgradeRequest, target, CancellationToken.None);
            await serverTask;
            await connection.Transport.Output.CompleteAsync();
            var clientBytes = await connection.ReadAllOutputAsync();
            var clientText = Encoding.ASCII.GetString(clientBytes);

            await Assert.That(clientText).Contains("HTTP/1.1 200 OK");
            await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
            await Assert.That(upgradeRequest.Flow.Status).IsEqualTo(TrafficFlowStatus.Complete);
            await Assert.That(bus.PublishedOf<ResponseReceived>().Count()).IsEqualTo(1);
            await Assert.That(bus.PublishedOf<TrafficFlowCompleted>().Count()).IsEqualTo(1);
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    ///     Verifies that when the upstream closes the socket without sending a valid response,
    ///     <see cref="HypertextTransferProtocolUpgradeOrchestrator.DispatchAsync" /> marks the
    ///     flow as failed and publishes <see cref="TrafficFlowCompleted" /> with that status.
    /// </summary>
    [Test]
    public async Task DispatchAsync_UpstreamClosesWithoutResponse_FailsFlow()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var serverTask = Task.Run(async () =>
        {
            using var acceptedClient = await listener.AcceptTcpClientAsync();
            var stream = acceptedClient.GetStream();
            await ConsumeRequestHeadersAsync(stream);
            acceptedClient.Close();
        });

        try
        {
            var bus = new StubDomainEventBus();
            var trafficStore = new StubTrafficStore();
            var orchestrator = BuildOrchestrator(bus, trafficStore);
            var connection = new StubFullDuplexProxyConnection();
            var upgradeRequest = BuildUpgradeRequest(connection);
            upgradeRequest.Flow.SetRequest(upgradeRequest.EffectiveRequest);
            var target = new ConnectTarget("127.0.0.1", port);

            await orchestrator.DispatchAsync(upgradeRequest, target, CancellationToken.None);
            await serverTask;

            await Assert.That(upgradeRequest.Flow.Status).IsEqualTo(TrafficFlowStatus.Failed);
            await Assert.That(bus.PublishedOf<TrafficFlowCompleted>().Count()).IsEqualTo(1);
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    ///     Verifies that a 101 response without a matching <c>Upgrade: websocket</c> header
    ///     completes the flow without trying to open a WebSocket tunnel.
    /// </summary>
    [Test]
    public async Task DispatchAsync_Upstream101NoWebSocketHeader_CompletesFlowWithoutTunnel()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var responseBytes = Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: h2c\r\nConnection: Upgrade\r\nContent-Length: 0\r\n\r\n");
        var serverTask = Task.Run(async () =>
        {
            using var acceptedClient = await listener.AcceptTcpClientAsync();
            using var stream = acceptedClient.GetStream();
            await ConsumeRequestHeadersAsync(stream);
            await stream.WriteAsync(responseBytes);
            await stream.FlushAsync();
        });

        try
        {
            var bus = new StubDomainEventBus();
            var trafficStore = new StubTrafficStore();
            var orchestrator = BuildOrchestrator(bus, trafficStore);
            var connection = new StubFullDuplexProxyConnection();
            var upgradeRequest = BuildUpgradeRequest(connection);
            upgradeRequest.Flow.SetRequest(upgradeRequest.EffectiveRequest);
            var target = new ConnectTarget("127.0.0.1", port);

            await orchestrator.DispatchAsync(upgradeRequest, target, CancellationToken.None);
            await serverTask;

            await Assert.That(upgradeRequest.Flow.Status).IsEqualTo(TrafficFlowStatus.Complete);
            await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
        }
        finally
        {
            listener.Stop();
        }
    }

    private static HypertextTransferProtocolUpgradeOrchestrator BuildOrchestrator(
        StubDomainEventBus bus,
        StubTrafficStore trafficStore)
    {
        var publisher = new HypertextTransferProtocolFlowEventPublisher(bus);
        var deps = new HypertextTransferProtocolUpgradeOrchestratorDependencies
        {
            FlowEventPublisher = publisher,
            HostResolver = null,
            TimeProvider = TimeProvider.System,
            TrafficStore = trafficStore,
            WebSocketStore = null,
        };
        return new HypertextTransferProtocolUpgradeOrchestrator(deps);
    }

    private static UpgradeExchangeRequest BuildUpgradeRequest(StubFullDuplexProxyConnection connection)
    {
        var headers = HeaderCollection.Empty
            .Add("Host", "example.com")
            .Add("Upgrade", "websocket")
            .Add("Connection", "Upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13");
        var requestData = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("http://example.com/chat"),
            Version = "HTTP/1.1",
        });
        var rawHeader = Encoding.ASCII.GetBytes("GET /chat HTTP/1.1\r\nHost: example.com\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n\r\n");
        var exchange = new HypertextTransferProtocolProxyRequestExchange(ReadOnlyMemory<byte>.Empty, rawHeader, requestData);
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:5050", DateTimeOffset.UtcNow);
        return UpgradeExchangeRequestFactory.Create(connection, requestData, flow, exchange);
    }

    private static async Task ConsumeRequestHeadersAsync(NetworkStream stream)
    {
        var buffer = new byte[1];
        var crlfState = 0;
        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 1));
            if (bytesRead == 0)
            {
                return;
            }

            var current = buffer[0];
            if (current == '\r')
            {
                if (crlfState is 0 or 2)
                {
                    crlfState++;
                }
                else
                {
                    crlfState = 1;
                }
                continue;
            }

            if (current == '\n')
            {
                if (crlfState is 1 or 3)
                {
                    crlfState++;
                }
                else
                {
                    crlfState = 0;
                }
                if (crlfState == 4)
                {
                    return;
                }
                continue;
            }

            crlfState = 0;
        }
    }
}
