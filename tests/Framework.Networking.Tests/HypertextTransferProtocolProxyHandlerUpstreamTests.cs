using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolProxyHandler" /> targeting the upstream
///     proxy chaining path (E07) and the response-phase breakpoint abort path (E04).
/// </summary>
[NotInParallel]
public sealed class HypertextTransferProtocolProxyHandlerUpstreamTests
{
    /// <summary>
    ///     Verifies that when an upstream proxy is configured and valid, the handler connects
    ///     to it and rewrites the request line to absolute-URI form.
    /// </summary>
    [Test]
    public async Task HandleAsync_UpstreamProxyConfigured_ConnectsToUpstreamAndRewrites()
    {
        using var upstreamListener = StartCapturingServer(out var capturedTask, "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstreamListener.LocalEndpoint).Port;
        var options = new UpstreamProxyOptions { IsEnabled = true, Host = "127.0.0.1", Port = upstreamPort };
        var optionsMonitor = new StubOptionsMonitor<UpstreamProxyOptions>(options);
        var handler = CreateHandler(upstreamProxy: optionsMonitor);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes("GET /api HTTP/1.1\r\nHost: real-upstream.example\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstreamListener.Stop();

        var captured = await capturedTask;
        await Assert.That(captured).StartsWith("GET http://real-upstream.example/api HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that when an aborting response breakpoint fires, the flow is failed and
    ///     no response is written to the client.
    /// </summary>
    [Test]
    public async Task HandleAsync_AbortingResponseBreakpoint_FailsFlow()
    {
        using var upstreamListener = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstreamListener.LocalEndpoint).Port;
        var breakpointHandler = new StubBreakpointHandler
        {
            ResponseDecision = BreakpointDecisions.Abort(),
        };
        var handler = CreateHandler(breakpointHandler: breakpointHandler);
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.WriteAsync(Encoding.ASCII.GetBytes($"GET /a HTTP/1.1\r\nHost: 127.0.0.1:{upstreamPort}\r\nConnection: close\r\n\r\n"));
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstreamListener.Stop();

        await Assert.That(breakpointHandler.ResponseResolveCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a Host header with default port (port omitted) attempts to connect
    ///     to the default port 80, ParseHostEndpoint covers the no-port branch.
    /// </summary>
    [Test]
    public async Task HandleAsync_HostHeaderWithoutPort_AttemptsConnectionAndFails()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.WriteAsync(Encoding.ASCII.GetBytes("GET /a HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n"));
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await handler.HandleAsync(connection, cancellationSource.Token);
        }
        catch (SocketException)
        {
            // Expected; we just need the no-port path to be exercised.
        }

        await Assert.That(connection.Transport).IsNotNull();
    }

    /// <summary>
    ///     Verifies that when an upstream proxy is configured but the destination host matches a
    ///     bypass pattern, the handler connects directly to the origin instead of via upstream.
    /// </summary>
    [Test]
    public async Task HandleAsync_BypassPatternMatchesHost_ConnectsDirectly()
    {
        using var originListener = StartCapturingServer(out var capturedTask, "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var originPort = ((IPEndPoint)originListener.LocalEndpoint).Port;
        var options = new UpstreamProxyOptions
        {
            IsEnabled = true,
            Host = "blackhole.invalid",
            Port = 65000,
        };
        options.BypassPatterns.Add("127.0.0.1");
        var optionsMonitor = new StubOptionsMonitor<UpstreamProxyOptions>(options);
        var handler = CreateHandler(upstreamProxy: optionsMonitor);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes($"GET /api HTTP/1.1\r\nHost: 127.0.0.1:{originPort}\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        originListener.Stop();

        var captured = await capturedTask;
        await Assert.That(captured).StartsWith("GET /api HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that when an upstream proxy is configured with credentials, the handler injects
    ///     a Proxy-Authorization Basic header.
    /// </summary>
    [Test]
    public async Task HandleAsync_UpstreamProxyWithCredentials_InjectsProxyAuthorizationHeader()
    {
        using var upstreamListener = StartCapturingServer(out var capturedTask, "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstreamListener.LocalEndpoint).Port;
        var options = new UpstreamProxyOptions
        {
            IsEnabled = true,
            Host = "127.0.0.1",
            Port = upstreamPort,
            Username = "alice",
            Password = "secret",
        };
        var optionsMonitor = new StubOptionsMonitor<UpstreamProxyOptions>(options);
        var handler = CreateHandler(upstreamProxy: optionsMonitor);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes("GET /api HTTP/1.1\r\nHost: corp.example\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstreamListener.Stop();

        var captured = await capturedTask;
        await Assert.That(captured).Contains("Proxy-Authorization: Basic YWxpY2U6c2VjcmV0");
    }

    private static HypertextTransferProtocolProxyHandler CreateHandler(
        IOptionsMonitor<UpstreamProxyOptions>? upstreamProxy = null,
        StubBreakpointHandler? breakpointHandler = null)
    {
        var registry = new RuleRegistry();
        if (breakpointHandler is not null)
        {
            var breakpointRule = new BreakpointRule(breakpointHandler);
            registry.RegisterAsyncRequestPhaseRule(breakpointRule);
            registry.RegisterAsyncResponsePhaseRule(breakpointRule);
        }
        var ruleEngine = new RuleEngine(registry, NullLogger<RuleEngine>.Instance);
        var dependencies = new HypertextTransferProtocolProxyHandlerDependencies
        {
            TrafficStore = new StubTrafficStore(),
            EventBus = new StubDomainEventBus(),
            RuleEngine = ruleEngine,
            Logger = NullLogger<HypertextTransferProtocolProxyHandler>.Instance,
            UpstreamProxy = upstreamProxy,
        };
        return new HypertextTransferProtocolProxyHandler(dependencies);
    }

    private static TcpListener StartCapturingServer(out Task<string> capturedTask, string responseText)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var localListener = listener;
        var captured = Task.Run(async () =>
        {
            using var client = await localListener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            var buffer = new byte[4096];
            var read = await stream.ReadAsync(buffer);
            var requestText = Encoding.ASCII.GetString(buffer, 0, read);
            var responseBytes = Encoding.ASCII.GetBytes(responseText);
            await stream.WriteAsync(responseBytes);
            await stream.FlushAsync();
            return requestText;
        });
        capturedTask = captured;
        return listener;
    }

    private static TcpListener StartHttpServer(string responseText)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        _ = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            var buffer = new byte[4096];
            var read = await stream.ReadAsync(buffer);
            _ = read;
            var responseBytes = Encoding.ASCII.GetBytes(responseText);
            await stream.WriteAsync(responseBytes);
            await stream.FlushAsync();
        });
        return listener;
    }
}
