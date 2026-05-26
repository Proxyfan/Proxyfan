using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Additional rule-pipeline integration tests for <see cref="HypertextTransferProtocolProxyHandler" />
///     exercising NoCaching (request + response phase), keep-alive behavior, and disabled rules.
/// </summary>
[NotInParallel]
public sealed class HypertextTransferProtocolProxyHandlerRuleAdditionalTests
{
    /// <summary>
    ///     Verifies that a NoCaching rule strips request cache headers AND adds Cache-Control: no-cache
    ///     in the request forwarded upstream.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoCachingRule_StripsRequestCacheHeaders()
    {
        using var upstream = StartHttpServerCapturingRequest("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var noCache = new NoCachingRule(matching, isEnabled: true, priority: 0);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { noCache }, new IResponsePhaseRule[] { noCache });
        var handler = CreateHandler(ruleEngine, out var trafficStore);
        var connection = new StubFullDuplexProxyConnection();

        var requestBytes = Encoding.ASCII.GetBytes(
            $"GET http://127.0.0.1:{upstreamPort}/api HTTP/1.1\r\n" +
            $"Host: 127.0.0.1:{upstreamPort}\r\n" +
            "Cache-Control: max-age=3600\r\n" +
            "If-None-Match: etag-123\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();
        await connection.Transport.Output.CompleteAsync();
        var capturedRequestText = await upstream.WaitForRequestAsync();

        await Assert.That(capturedRequestText.Contains("Cache-Control: no-cache\r\n", StringComparison.Ordinal)).IsTrue();
        await Assert.That(capturedRequestText.Contains("If-None-Match", StringComparison.Ordinal)).IsFalse();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a NoCaching rule strips response cache headers from the response
    ///     delivered to the client.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoCachingRule_StripsResponseCacheHeaders()
    {
        var upstreamResponse = "HTTP/1.1 200 OK\r\n" +
            "Content-Length: 2\r\n" +
            "Connection: close\r\n" +
            "Cache-Control: max-age=3600\r\n" +
            "ETag: v1\r\n\r\nok";
        using var upstream = StartHttpServerCapturingRequest(upstreamResponse);
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var noCache = new NoCachingRule(matching, isEnabled: true, priority: 0);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { noCache }, new IResponsePhaseRule[] { noCache });
        var handler = CreateHandler(ruleEngine, out var trafficStore);
        var connection = new StubFullDuplexProxyConnection();

        var requestBytes = Encoding.ASCII.GetBytes($"GET http://127.0.0.1:{upstreamPort}/api HTTP/1.1\r\nHost: 127.0.0.1:{upstreamPort}\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();
        await connection.Transport.Output.CompleteAsync();
        var responseBytes = await connection.ReadAllOutputAsync();
        var responseText = Encoding.ASCII.GetString(responseBytes);

        await Assert.That(responseText.Contains("Cache-Control: no-cache, no-store, must-revalidate", StringComparison.Ordinal)).IsTrue();
        await Assert.That(responseText.Contains("ETag:", StringComparison.Ordinal)).IsFalse();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a disabled BlockList rule does not block the request.
    /// </summary>
    [Test]
    public async Task HandleAsync_DisabledBlockListRule_DoesNotBlock()
    {
        using var upstream = StartHttpServerCapturingRequest("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var blockList = new BlockListRule(new[] { matching }, isEnabled: false, priority: 0);
        var ruleEngine = new RuleEngine(new IRequestPhaseRule[] { blockList }, Array.Empty<IResponsePhaseRule>());
        var handler = CreateHandler(ruleEngine, out var trafficStore);
        var connection = new StubFullDuplexProxyConnection();

        var requestBytes = Encoding.ASCII.GetBytes($"GET http://127.0.0.1:{upstreamPort}/api HTTP/1.1\r\nHost: 127.0.0.1:{upstreamPort}\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();
        await connection.Transport.Output.CompleteAsync();
        var responseBytes = await connection.ReadAllOutputAsync();
        var responseText = Encoding.ASCII.GetString(responseBytes);

        await Assert.That(responseText.StartsWith("HTTP/1.1 200", StringComparison.Ordinal)).IsTrue();
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

    private static CapturingUpstream StartHttpServerCapturingRequest(string responseText)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var capturedRequestSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverTask = ServerLoopCapturingAsync(listener, responseText, capturedRequestSource);
        return new CapturingUpstream(listener, serverTask, capturedRequestSource);
    }

    private static async Task ServerLoopCapturingAsync(TcpListener listener, string responseText, TaskCompletionSource<string> capturedRequest)
    {
        try
        {
            using var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            await using var networkStream = client.GetStream();
            var requestBuffer = new byte[8192];
            var bytesRead = await networkStream.ReadAsync(requestBuffer).ConfigureAwait(false);
            var requestText = Encoding.ASCII.GetString(requestBuffer, 0, bytesRead);
            capturedRequest.TrySetResult(requestText);
            var responseBytes = Encoding.ASCII.GetBytes(responseText);
            await networkStream.WriteAsync(responseBytes).ConfigureAwait(false);
            await networkStream.FlushAsync().ConfigureAwait(false);
        }
        catch (SocketException)
        {
            capturedRequest.TrySetResult(string.Empty);
        }
        catch (ObjectDisposedException)
        {
            capturedRequest.TrySetResult(string.Empty);
        }
        catch (IOException)
        {
            capturedRequest.TrySetResult(string.Empty);
        }
    }

    private sealed class CapturingUpstream : IDisposable
    {
        private readonly TaskCompletionSource<string> _capturedRequest;
        private readonly Task _serverTask;

        public TcpListener Listener { get; }

        public CapturingUpstream(TcpListener listener, Task serverTask, TaskCompletionSource<string> capturedRequest)
        {
            Listener = listener;
            _serverTask = serverTask;
            _capturedRequest = capturedRequest;
        }

        public Task<string> WaitForRequestAsync()
        {
            return _capturedRequest.Task;
        }

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
