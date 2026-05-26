using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolProxyHandler" />.
/// </summary>
public sealed class HypertextTransferProtocolProxyHandlerTests
{
    /// <summary>
    ///     Verifies that <c>CanHandle</c> returns <see langword="true" /> for all standard HTTP method
    ///     prefixes.
    /// </summary>
    /// <param name="methodPrefix">
    ///     The HTTP method prefix to test.
    /// </param>
    [Test]
    [Arguments("DELETE ")]
    [Arguments("HEAD ")]
    [Arguments("OPTIONS ")]
    [Arguments("PATCH ")]
    [Arguments("POST ")]
    [Arguments("PUT ")]
    [Arguments("TRACE ")]
    public async Task CanHandle_AllHttpMethodPrefixes_ReturnsTrue(string methodPrefix)
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var logger = new StubLogger<HypertextTransferProtocolProxyHandler>();
        var handler = new HypertextTransferProtocolProxyHandler(trafficStore, eventBus, logger);
        var bytes = Encoding.ASCII.GetBytes(methodPrefix + "/path HTTP/1.1\r\n");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that <c>CanHandle</c> returns <see langword="false" /> for CONNECT method bytes.
    /// </summary>
    [Test]
    public async Task CanHandle_ConnectBytes_ReturnsFalse()
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var logger = new StubLogger<HypertextTransferProtocolProxyHandler>();
        var handler = new HypertextTransferProtocolProxyHandler(trafficStore, eventBus, logger);
        var bytes = Encoding.ASCII.GetBytes("CONNECT example.com:443 HTTP/1.1\r\n");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that <c>CanHandle</c> returns <see langword="false" /> for an empty byte sequence.
    /// </summary>
    [Test]
    public async Task CanHandle_EmptySequence_ReturnsFalse()
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var logger = new StubLogger<HypertextTransferProtocolProxyHandler>();
        var handler = new HypertextTransferProtocolProxyHandler(trafficStore, eventBus, logger);
        var sequence = ReadOnlySequence<byte>.Empty;

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that <c>CanHandle</c> returns <see langword="true" /> for GET request bytes.
    /// </summary>
    [Test]
    public async Task CanHandle_GetBytes_ReturnsTrue()
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var logger = new StubLogger<HypertextTransferProtocolProxyHandler>();
        var handler = new HypertextTransferProtocolProxyHandler(trafficStore, eventBus, logger);
        var bytes = Encoding.ASCII.GetBytes("GET /path HTTP/1.1\r\n");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that <c>CanHandle</c> returns <see langword="false" /> for unrecognized bytes.
    /// </summary>
    [Test]
    public async Task CanHandle_UnknownBytes_ReturnsFalse()
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var logger = new StubLogger<HypertextTransferProtocolProxyHandler>();
        var handler = new HypertextTransferProtocolProxyHandler(trafficStore, eventBus, logger);
        var bytes = Encoding.ASCII.GetBytes("\x16\x03\x01\x00\xf1");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that <c>HandleAsync</c> returns without storing a flow when the input pipe
    ///     is completed without any data.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyInput_ReturnsWithoutStoringFlow()
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var logger = new StubLogger<HypertextTransferProtocolProxyHandler>();
        var handler = new HypertextTransferProtocolProxyHandler(trafficStore, eventBus, logger);
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.CompleteAsync().ConfigureAwait(false);

        await handler.HandleAsync(connection, CancellationToken.None).ConfigureAwait(false);

        await Assert.That(trafficStore.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <c>HandleAsync</c> forwards a GET request to an upstream server, stores the
    ///     completed flow, and publishes all expected domain events.
    /// </summary>
    [Test]
    public async Task HandleAsync_GetRequest_StoresCompletedFlowAndPublishesEvents()
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var logger = new StubLogger<HypertextTransferProtocolProxyHandler>();
        var handler = new HypertextTransferProtocolProxyHandler(trafficStore, eventBus, logger);
        var upstreamListener = new TcpListener(IPAddress.Loopback, 0);
        upstreamListener.Start();

        try
        {
            var upstreamPort = ((IPEndPoint)upstreamListener.LocalEndpoint).Port;
            var connection = new StubFullDuplexProxyConnection();
            var requestText = "GET http://127.0.0.1:" + upstreamPort + "/hello HTTP/1.1\r\n"
                + "Host: 127.0.0.1:" + upstreamPort + "\r\n"
                + "Connection: close\r\n"
                + "\r\n";
            var requestBytes = Encoding.ASCII.GetBytes(requestText);
            await connection.InputWriter.WriteAsync(requestBytes).ConfigureAwait(false);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var serverTask = RunUpstreamServerAsync(upstreamListener, cancellationTokenSource.Token);
            await handler.HandleAsync(connection, cancellationTokenSource.Token).ConfigureAwait(false);
            await serverTask.ConfigureAwait(false);

            await Assert.That(trafficStore.Count).IsEqualTo(1);
            await Assert.That(eventBus.Published.Count).IsGreaterThan(0);
        }
        finally
        {
            upstreamListener.Stop();
        }
    }

    /// <summary>
    ///     Verifies that <c>HandleAsync</c> fails the flow and returns without storing it when the
    ///     request is missing a <c>Host</c> header.
    /// </summary>
    [Test]
    public async Task HandleAsync_RequestMissingHostHeader_FailsFlowWithoutStoringIt()
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var logger = new StubLogger<HypertextTransferProtocolProxyHandler>();
        var handler = new HypertextTransferProtocolProxyHandler(trafficStore, eventBus, logger);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes("GET /path HTTP/1.1\r\nAccept: */*\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes).ConfigureAwait(false);
        await connection.InputWriter.CompleteAsync().ConfigureAwait(false);

        await handler.HandleAsync(connection, CancellationToken.None).ConfigureAwait(false);

        await Assert.That(trafficStore.Count).IsEqualTo(0);
        var completedEvents = eventBus.PublishedOf<TrafficFlowCompleted>();
        await Assert.That(completedEvents).HasSingleItem();
    }

    private static async Task RunUpstreamServerAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        await using var networkStream = client.GetStream();
        var requestBuffer = new byte[4096];
        await networkStream.ReadAsync(requestBuffer, cancellationToken).ConfigureAwait(false);
        var responseText = "HTTP/1.1 200 OK\r\n"
            + "Content-Length: 2\r\n"
            + "Connection: close\r\n"
            + "\r\n"
            + "OK";
        var responseBytes = Encoding.ASCII.GetBytes(responseText);
        await networkStream.WriteAsync(responseBytes, cancellationToken).ConfigureAwait(false);
        await networkStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}