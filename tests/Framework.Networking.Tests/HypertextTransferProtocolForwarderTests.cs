using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolForwarder" /> covering the standard
///     read-and-return path, the SSE streaming-relay branch, and the upstream-failure path.
///     Each test stands up a real loopback TCP listener that pretends to be the upstream
///     server.
/// </summary>
public sealed class HypertextTransferProtocolForwarderTests
{
    /// <summary>
    ///     When the upstream returns a regular response, the forwarder yields a
    ///     <see cref="HypertextTransferProtocolForwardingOutcomes" /> standard exchange that
    ///     carries the parsed response data.
    /// </summary>
    [Test]
    public async Task ForwardAsync_UpstreamReturnsContentLengthBody_ReturnsStandardOutcome()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var responseBytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nhello");
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
            var forwarder = BuildForwarder();
            var (connection, request) = BuildForwardingRequest();
            request.Flow.SetRequest(request.EffectiveRequest);
            var target = new ConnectTarget("127.0.0.1", port);

            var outcome = await forwarder.ForwardAsync(request, target, CancellationToken.None);
            await serverTask;

            await Assert.That(outcome.IsFailure).IsFalse();
            await Assert.That(outcome.IsStreaming).IsFalse();
            await Assert.That(outcome.Exchange).IsNotNull();
            await Assert.That(outcome.Exchange!.Response.StatusCode).IsEqualTo(200);
            _ = connection;
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    ///     When the upstream returns a <c>text/event-stream</c> response, the forwarder hands
    ///     the body off to the SSE relay (returning a Streamed outcome) and the captured flow
    ///     is added to the traffic store.
    /// </summary>
    [Test]
    public async Task ForwardAsync_UpstreamReturnsServerSentEventsStream_ReturnsStreamedOutcomeAndCapturesFlow()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var responseHead = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/event-stream\r\nCache-Control: no-cache\r\n\r\n");
        var eventBytes = Encoding.UTF8.GetBytes("data: ping\n\n");
        var serverTask = Task.Run(async () =>
        {
            using var acceptedClient = await listener.AcceptTcpClientAsync();
            using var stream = acceptedClient.GetStream();
            await ConsumeRequestHeadersAsync(stream);
            await stream.WriteAsync(responseHead);
            await stream.WriteAsync(eventBytes);
            await stream.FlushAsync();
        });

        try
        {
            var trafficStore = new StubTrafficStore();
            var sseStore = new ServerSentEventsStore(capacity: 4);
            var forwarder = BuildForwarder(trafficStore: trafficStore, serverSentEventsStore: sseStore);
            var (connection, request) = BuildForwardingRequest();
            request.Flow.SetRequest(request.EffectiveRequest);
            var target = new ConnectTarget("127.0.0.1", port);

            var outcome = await forwarder.ForwardAsync(request, target, CancellationToken.None);
            await serverTask;

            await Assert.That(outcome.IsStreaming).IsTrue();
            await Assert.That(outcome.Exchange).IsNull();
            await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
            await Assert.That(sseStore.GetAll().Count).IsEqualTo(1);
            _ = connection;
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    ///     When the upstream closes the connection without sending response headers, the
    ///     forwarder returns a failure outcome instead of throwing.
    /// </summary>
    [Test]
    public async Task ForwardAsync_UpstreamClosesBeforeResponse_ReturnsFailureOutcome()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var serverTask = Task.Run(async () =>
        {
            using var acceptedClient = await listener.AcceptTcpClientAsync();
            using var stream = acceptedClient.GetStream();
            await ConsumeRequestHeadersAsync(stream);
            acceptedClient.Close();
        });

        try
        {
            var forwarder = BuildForwarder();
            var (connection, request) = BuildForwardingRequest();
            request.Flow.SetRequest(request.EffectiveRequest);
            var target = new ConnectTarget("127.0.0.1", port);

            var outcome = await forwarder.ForwardAsync(request, target, CancellationToken.None);
            await serverTask;

            await Assert.That(outcome.IsFailure).IsTrue();
            _ = connection;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static HypertextTransferProtocolForwarder BuildForwarder(
        StubTrafficStore? trafficStore = null,
        ServerSentEventsStore? serverSentEventsStore = null)
    {
        var dependencies = new HypertextTransferProtocolForwarderDependencies
        {
            EventBus = new StubDomainEventBus(),
            HostResolver = null,
            Logger = NullLogger.Instance,
            ServerSentEventsStore = serverSentEventsStore,
            ThrottleProfile = null,
            TimeProvider = TimeProvider.System,
            TrafficStore = trafficStore ?? new StubTrafficStore(),
            UpstreamProxy = null,
        };
        return new HypertextTransferProtocolForwarder(dependencies);
    }

    private static (StubFullDuplexProxyConnection Connection, HypertextTransferProtocolForwardingRequest Request) BuildForwardingRequest()
    {
        var connection = new StubFullDuplexProxyConnection();
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var requestData = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("http://example.com/test"),
            Version = "HTTP/1.1",
        });
        var rawHeader = Encoding.ASCII.GetBytes("GET /test HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var requestExchange = new HypertextTransferProtocolProxyRequestExchange(ReadOnlyMemory<byte>.Empty, rawHeader, requestData);
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:5005", DateTimeOffset.UtcNow);
        return (connection, new HypertextTransferProtocolForwardingRequest
        {
            Connection = connection,
            EffectiveRequest = requestData,
            Flow = flow,
            RequestExchange = requestExchange,
        });
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
