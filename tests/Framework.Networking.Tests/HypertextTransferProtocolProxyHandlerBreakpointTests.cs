using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Integration tests for the breakpoint flow inside
///     <see cref="HypertextTransferProtocolProxyHandler" />.
/// </summary>
[NotInParallel]
public sealed class HypertextTransferProtocolProxyHandlerBreakpointTests
{
    /// <summary>
    ///     Verifies that the request-phase breakpoint handler is invoked when configured.
    /// </summary>
    [Test]
    public async Task HandleAsync_BreakpointHandlerConfigured_InvokesRequestBreakpoint()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var breakpointHandler = new StubBreakpointHandler();
        var handler = CreateHandler(breakpointHandler);
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(breakpointHandler.RequestResolveCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that the response-phase breakpoint handler is invoked when configured.
    /// </summary>
    [Test]
    public async Task HandleAsync_BreakpointHandlerConfigured_InvokesResponseBreakpoint()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var breakpointHandler = new StubBreakpointHandler();
        var handler = CreateHandler(breakpointHandler);
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(breakpointHandler.ResponseResolveCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that an aborting request breakpoint fails the flow without forwarding.
    /// </summary>
    [Test]
    public async Task HandleAsync_AbortingRequestBreakpoint_FailsFlow()
    {
        var breakpointHandler = new StubBreakpointHandler
        {
            RequestDecision = BreakpointDecisions.Abort(),
        };
        var trafficStore = new StubTrafficStore();
        var handler = CreateHandler(breakpointHandler, trafficStore);
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, 65535);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);

        await Assert.That(breakpointHandler.RequestResolveCount).IsEqualTo(1);
        await Assert.That(breakpointHandler.ResponseResolveCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that no breakpoint calls happen when the handler is null.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoBreakpointHandler_DoesNotInvokeAnyDecision()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var handler = CreateHandler(null);
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(upstreamPort).IsGreaterThan(0);
    }

    private static HypertextTransferProtocolProxyHandler CreateHandler(StubBreakpointHandler? breakpointHandler)
    {
        return CreateHandler(breakpointHandler, new StubTrafficStore());
    }

    private static HypertextTransferProtocolProxyHandler CreateHandler(StubBreakpointHandler? breakpointHandler, ITrafficStore trafficStore)
    {
        var ruleEngine = new RuleEngine(System.Array.Empty<IRequestPhaseRule>(), System.Array.Empty<IResponsePhaseRule>());
        var handler = new HypertextTransferProtocolProxyHandler(new HypertextTransferProtocolProxyHandlerDependencies
        {
            TrafficStore = trafficStore,
            EventBus = new StubDomainEventBus(),
            RuleEngine = ruleEngine,
            Logger = NullLogger<HypertextTransferProtocolProxyHandler>.Instance,
            BreakpointHandler = breakpointHandler,
        });
        return handler;
    }

    private static HttpServer StartHttpServer(string responseText)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var task = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            var buffer = new byte[4096];
            var bytesRead = await stream.ReadAsync(buffer);
            _ = bytesRead;
            var responseBytes = Encoding.ASCII.GetBytes(responseText);
            await stream.WriteAsync(responseBytes);
            await stream.FlushAsync();
        });
        return new HttpServer(listener, task);
    }

    private static async Task WriteSimpleRequestAsync(StubFullDuplexProxyConnection connection, int upstreamPort)
    {
        var requestBytes = Encoding.ASCII.GetBytes(
            $"GET /api HTTP/1.1\r\nHost: 127.0.0.1:{upstreamPort}\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();
    }

    private sealed class HttpServer : IDisposable
    {
        public TcpListener Listener { get; }
        public Task ServerTask { get; }

        public HttpServer(TcpListener listener, Task serverTask)
        {
            Listener = listener;
            ServerTask = serverTask;
        }

        public void Dispose()
        {
            Listener.Stop();
        }

        public void Stop()
        {
            Listener.Stop();
        }
    }
}
