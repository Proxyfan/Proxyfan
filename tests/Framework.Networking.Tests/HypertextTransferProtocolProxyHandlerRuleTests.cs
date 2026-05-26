using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Integration tests for <see cref="HypertextTransferProtocolProxyHandler" /> exercising
///     the rule-engine pipeline (Block, Map Local, Map Remote, No Caching).
/// </summary>
[NotInParallel]
public sealed class HypertextTransferProtocolProxyHandlerRuleTests
{
    /// <summary>
    ///     Verifies that a BlockList match short-circuits the pipeline and writes a 403 response.
    /// </summary>
    [Test]
    public async Task HandleAsync_BlockListMatch_Writes403AndDoesNotForward()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var blockList = new BlockListRule(new[] { matching }, isEnabled: true, priority: 0);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { blockList }, Array.Empty<IResponsePhaseRule>());
        var handler = CreateHandler(ruleEngine, out var trafficStore);
        var connection = new StubFullDuplexProxyConnection();

        var requestBytes = Encoding.ASCII.GetBytes("GET http://blocked.example/ HTTP/1.1\r\nHost: blocked.example\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var responseBytes = await connection.ReadAllOutputAsync();
        var responseText = Encoding.ASCII.GetString(responseBytes);

        await Assert.That(responseText.StartsWith("HTTP/1.1 403", StringComparison.Ordinal)).IsTrue();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
        await Assert.That(trafficStore.AddedFlows[0].Response!.StatusCode).IsEqualTo(403);
    }

    /// <summary>
    ///     Verifies that a Map Local rule short-circuits the pipeline and serves a configured response.
    /// </summary>
    [Test]
    public async Task HandleAsync_MapLocalMatch_WritesLocalResponse()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var parameters = new MapLocalRuleParameters
        {
            Body = Encoding.UTF8.GetBytes("hello local"),
            Headers = new[] { new KeyValuePair<string, string>("Content-Type", "text/plain"), new KeyValuePair<string, string>("Content-Length", "11") },
            IsEnabled = true,
            Priority = 0,
            ReasonPhrase = "OK",
            StatusCode = 200,
        };
        var mapLocal = new MapLocalRule(matching, parameters);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { mapLocal }, Array.Empty<IResponsePhaseRule>());
        var handler = CreateHandler(ruleEngine, out var trafficStore);
        var connection = new StubFullDuplexProxyConnection();

        var requestBytes = Encoding.ASCII.GetBytes("GET http://anything.example/ HTTP/1.1\r\nHost: anything.example\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var responseBytes = await connection.ReadAllOutputAsync();
        var responseText = Encoding.ASCII.GetString(responseBytes);

        await Assert.That(responseText.StartsWith("HTTP/1.1 200", StringComparison.Ordinal)).IsTrue();
        await Assert.That(responseText.Contains("hello local", StringComparison.Ordinal)).IsTrue();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that an AllowList that does not match the request also writes a 403 response.
    /// </summary>
    [Test]
    public async Task HandleAsync_AllowListNoMatch_Writes403()
    {
        var matching = new MatchingRule("https://allowed.example/*", MatchingRuleKind.Wildcard);
        var allowList = new AllowListRule(new[] { matching }, isEnabled: true, priority: 0);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { allowList }, Array.Empty<IResponsePhaseRule>());
        var handler = CreateHandler(ruleEngine, out var trafficStore);
        var connection = new StubFullDuplexProxyConnection();

        var requestBytes = Encoding.ASCII.GetBytes("GET http://other.example/ HTTP/1.1\r\nHost: other.example\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var responseBytes = await connection.ReadAllOutputAsync();
        var responseText = Encoding.ASCII.GetString(responseBytes);

        await Assert.That(responseText.StartsWith("HTTP/1.1 403", StringComparison.Ordinal)).IsTrue();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that Map Remote rewrites the upstream destination so that requests reach a different
    ///     real upstream server.
    /// </summary>
    [Test]
    public async Task HandleAsync_MapRemoteMatch_ForwardsToRewrittenDestination()
    {
        using var upstreamListener = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 8\r\nConnection: close\r\n\r\nremapped");
        var upstreamPort = ((IPEndPoint)upstreamListener.Listener.LocalEndpoint).Port;

        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(
            scheme: null,
            host: "127.0.0.1",
            port: upstreamPort,
            path: null,
            isPreservingHostHeader: false);
        var mapRemote = new MapRemoteRule(matching, destination, isEnabled: true, priority: 0);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { mapRemote }, Array.Empty<IResponsePhaseRule>());
        var handler = CreateHandler(ruleEngine, out var trafficStore);
        var connection = new StubFullDuplexProxyConnection();

        var requestBytes = Encoding.ASCII.GetBytes("GET http://prod.example/api HTTP/1.1\r\nHost: prod.example\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstreamListener.Stop();
        await connection.Transport.Output.CompleteAsync();
        var responseBytes = await connection.ReadAllOutputAsync();
        var responseText = Encoding.ASCII.GetString(responseBytes);

        await Assert.That(responseText.StartsWith("HTTP/1.1 200", StringComparison.Ordinal)).IsTrue();
        await Assert.That(responseText.Contains("remapped", StringComparison.Ordinal)).IsTrue();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
    }

    private static HypertextTransferProtocolProxyHandler CreateHandler(IRuleEngine ruleEngine, out StubTrafficStore trafficStore)
    {
        var newStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var handler = new HypertextTransferProtocolProxyHandler(
            newStore,
            eventBus,
            ruleEngine,
            NullLogger<HypertextTransferProtocolProxyHandler>.Instance);
        trafficStore = newStore;
        return handler;
    }

    private static UpstreamListener StartHttpServer(string responseText)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var serverTask = ServerLoopAsync(listener, responseText);
        return new UpstreamListener(listener, serverTask);
    }

    private static async Task ServerLoopAsync(TcpListener listener, string responseText)
    {
        try
        {
            using var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            await using var networkStream = client.GetStream();
            var requestBuffer = new byte[4096];
            await networkStream.ReadAsync(requestBuffer).ConfigureAwait(false);
            var responseBytes = Encoding.ASCII.GetBytes(responseText);
            await networkStream.WriteAsync(responseBytes).ConfigureAwait(false);
            await networkStream.FlushAsync().ConfigureAwait(false);
        }
        catch (SocketException)
        {
            // Expected on shutdown.
        }
        catch (ObjectDisposedException)
        {
            // Expected on shutdown.
        }
        catch (System.IO.IOException)
        {
            // Expected on connection close.
        }
    }

    private sealed class UpstreamListener : IDisposable
    {
        private readonly Task _serverTask;

        public UpstreamListener(TcpListener listener, Task serverTask)
        {
            Listener = listener;
            _serverTask = serverTask;
        }

        public TcpListener Listener { get; }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            try
            {
                Listener.Stop();
            }
            catch (SocketException)
            {
                // Ignored on shutdown.
            }
        }
    }
}
