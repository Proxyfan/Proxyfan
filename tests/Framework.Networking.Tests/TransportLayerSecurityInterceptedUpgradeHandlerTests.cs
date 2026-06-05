using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptedUpgradeHandler" /> verifying
///     the wss:// upgrade orchestration: the rewritten request reaches the upstream pipe,
///     an upstream 101 switches to bidirectional tunneling and records the WebSocket flow,
///     a non-101 upstream response short-circuits before tunneling, and a missing upstream
///     response fails the flow without storing it.
/// </summary>
public sealed class TransportLayerSecurityInterceptedUpgradeHandlerTests
{
    /// <summary>
    ///     A successful upstream 101 Switching Protocols response triggers the WebSocket
    ///     tunnel and records the flow as completed.
    /// </summary>
    [Test]
    public async Task HandleAsync_SuccessfulUpgrade_RecordsFlowAndTunnels()
    {
        var pipes = BuildPipes(
            out var clientIngressWriter,
            out _,
            out var serverIngressWriter,
            out _);
        var upstreamResponse = "HTTP/1.1 101 Switching Protocols\r\n"
            + "Upgrade: websocket\r\n"
            + "Connection: Upgrade\r\n"
            + "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n"
            + "\r\n";
        await serverIngressWriter.WriteAsync(Encoding.ASCII.GetBytes(upstreamResponse), CancellationToken.None);
        await serverIngressWriter.CompleteAsync();
        await clientIngressWriter.CompleteAsync();

        var clientTunnelStream = new DuplexStream(new MemoryStream(BuildCloseFrame()), new MemoryStream());
        var serverTunnelStream = new DuplexStream(new MemoryStream(BuildCloseFrame()), new MemoryStream());

        var trafficStore = new StubTrafficStore();
        var webSocketStore = new WebSocketStore();
        var eventBus = new StubDomainEventBus();
        var handler = new TransportLayerSecurityInterceptedUpgradeHandler(
            BuildDependencies(eventBus, trafficStore, webSocketStore));

        var request = BuildUpgradeRequest(pipes, clientTunnelStream, serverTunnelStream);

        await handler.HandleAsync(request, CancellationToken.None);

        await Assert.That(trafficStore.Count).IsEqualTo(1);
        await Assert.That(webSocketStore.Count).IsEqualTo(1);
        await Assert.That(trafficStore.GetAll()[0].Status).IsEqualTo(TrafficFlowStatus.Complete);
    }

    /// <summary>
    ///     A non-101 upstream response (e.g., 426 Upgrade Required) is forwarded to the
    ///     client and the flow is recorded as completed without launching the tunnel.
    /// </summary>
    [Test]
    public async Task HandleAsync_UpstreamRejectsUpgrade_CompletesFlowWithoutTunnel()
    {
        var pipes = BuildPipes(
            out var clientIngressWriter,
            out _,
            out var serverIngressWriter,
            out _);
        var upstreamResponse = "HTTP/1.1 426 Upgrade Required\r\nContent-Length: 0\r\n\r\n";
        await serverIngressWriter.WriteAsync(Encoding.ASCII.GetBytes(upstreamResponse), CancellationToken.None);
        await serverIngressWriter.CompleteAsync();
        await clientIngressWriter.CompleteAsync();

        var clientTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());
        var serverTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());

        var trafficStore = new StubTrafficStore();
        var webSocketStore = new WebSocketStore();
        var eventBus = new StubDomainEventBus();
        var handler = new TransportLayerSecurityInterceptedUpgradeHandler(
            BuildDependencies(eventBus, trafficStore, webSocketStore));

        var request = BuildUpgradeRequest(pipes, clientTunnelStream, serverTunnelStream);

        await handler.HandleAsync(request, CancellationToken.None);

        await Assert.That(trafficStore.Count).IsEqualTo(1);
        await Assert.That(webSocketStore.Count).IsEqualTo(0);
        await Assert.That(trafficStore.GetAll()[0].Status).IsEqualTo(TrafficFlowStatus.Complete);
    }

    /// <summary>
    ///     When the upstream closes without writing any response, the flow is marked Failed
    ///     and not added to the traffic store.
    /// </summary>
    [Test]
    public async Task HandleAsync_UpstreamWritesNothing_FailsFlow()
    {
        var pipes = BuildPipes(
            out var clientIngressWriter,
            out _,
            out var serverIngressWriter,
            out _);
        await serverIngressWriter.CompleteAsync();
        await clientIngressWriter.CompleteAsync();

        var clientTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());
        var serverTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());

        var trafficStore = new StubTrafficStore();
        var webSocketStore = new WebSocketStore();
        var eventBus = new StubDomainEventBus();
        var handler = new TransportLayerSecurityInterceptedUpgradeHandler(
            BuildDependencies(eventBus, trafficStore, webSocketStore));

        var request = BuildUpgradeRequest(pipes, clientTunnelStream, serverTunnelStream);

        await handler.HandleAsync(request, CancellationToken.None);

        await Assert.That(trafficStore.Count).IsEqualTo(0);
        await Assert.That(webSocketStore.Count).IsEqualTo(0);
        await Assert.That(request.Flow.Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>
    ///     A successful upgrade still tunnels when no WebSocket store is provided.
    /// </summary>
    [Test]
    public async Task HandleAsync_NullWebSocketStore_StillTunnels()
    {
        var pipes = BuildPipes(
            out var clientIngressWriter,
            out _,
            out var serverIngressWriter,
            out _);
        var upstreamResponse = "HTTP/1.1 101 Switching Protocols\r\n"
            + "Upgrade: websocket\r\n"
            + "Connection: Upgrade\r\n"
            + "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n"
            + "\r\n";
        await serverIngressWriter.WriteAsync(Encoding.ASCII.GetBytes(upstreamResponse), CancellationToken.None);
        await serverIngressWriter.CompleteAsync();
        await clientIngressWriter.CompleteAsync();

        var clientTunnelStream = new DuplexStream(new MemoryStream(BuildCloseFrame()), new MemoryStream());
        var serverTunnelStream = new DuplexStream(new MemoryStream(BuildCloseFrame()), new MemoryStream());

        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var handler = new TransportLayerSecurityInterceptedUpgradeHandler(
            BuildDependencies(eventBus, trafficStore, webSocketStore: null));

        var request = BuildUpgradeRequest(pipes, clientTunnelStream, serverTunnelStream);

        await handler.HandleAsync(request, CancellationToken.None);

        await Assert.That(trafficStore.Count).IsEqualTo(1);
        await Assert.That(trafficStore.GetAll()[0].Status).IsEqualTo(TrafficFlowStatus.Complete);
    }

    /// <summary>
    ///     A response-phase rule (No Caching) is evaluated against the upstream 101 upgrade
    ///     response and its strip-headers modification reaches both the wire bytes sent to
    ///     the client and the stored flow.
    /// </summary>
    [Test]
    public async Task HandleAsync_SuccessfulUpgrade_AppliesResponsePhaseRule()
    {
        var pipes = BuildPipes(
            out var clientIngressWriter,
            out var clientEgressReader,
            out var serverIngressWriter,
            out _);
        var upstreamResponse = "HTTP/1.1 101 Switching Protocols\r\n"
            + "Upgrade: websocket\r\n"
            + "Connection: Upgrade\r\n"
            + "Sec-WebSocket-Accept: dGVzdA==\r\n"
            + "Cache-Control: max-age=600\r\n"
            + "\r\n";
        await serverIngressWriter.WriteAsync(Encoding.ASCII.GetBytes(upstreamResponse), CancellationToken.None);
        await serverIngressWriter.CompleteAsync();
        await clientIngressWriter.CompleteAsync();

        var clientTunnelStream = new DuplexStream(new MemoryStream(BuildCloseFrame()), new MemoryStream());
        var serverTunnelStream = new DuplexStream(new MemoryStream(BuildCloseFrame()), new MemoryStream());

        var trafficStore = new StubTrafficStore();
        var webSocketStore = new WebSocketStore();
        var eventBus = new StubDomainEventBus();
        var noCaching = new NoCachingRule(new MatchingRule("*", MatchingRuleKind.Wildcard), isEnabled: true, priority: 0);
        var ruleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), new IResponsePhaseRule[] { noCaching });
        var dependencies = BuildDependencies(eventBus, trafficStore, webSocketStore, ruleEngine: ruleEngine);
        var handler = new TransportLayerSecurityInterceptedUpgradeHandler(dependencies);

        var request = BuildUpgradeRequest(pipes, clientTunnelStream, serverTunnelStream);

        await handler.HandleAsync(request, CancellationToken.None);

        var wireBytes = await ReadAvailableAsync(clientEgressReader);
        var wireText = Encoding.ASCII.GetString(wireBytes);

        await Assert.That(wireText.Contains("Cache-Control: no-cache", StringComparison.OrdinalIgnoreCase)).IsTrue();
        await Assert.That(wireText.Contains("max-age=600", StringComparison.OrdinalIgnoreCase)).IsFalse();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
        var storedResponse = trafficStore.GetAll()[0].Response!;
        await Assert.That(storedResponse.Headers.Get("Cache-Control")).IsEqualTo("no-cache, no-store, must-revalidate");
    }

    /// <summary>
    ///     A response-phase rule is evaluated against a non-101 upstream upgrade response
    ///     too, so policies behave identically for rejected TLS upgrades and normal HTTPS
    ///     responses.
    /// </summary>
    [Test]
    public async Task HandleAsync_UpstreamRejectsUpgrade_AppliesResponsePhaseRule()
    {
        var pipes = BuildPipes(
            out var clientIngressWriter,
            out var clientEgressReader,
            out var serverIngressWriter,
            out _);
        var upstreamResponse = "HTTP/1.1 426 Upgrade Required\r\n"
            + "Cache-Control: max-age=600\r\n"
            + "Content-Length: 0\r\n"
            + "\r\n";
        await serverIngressWriter.WriteAsync(Encoding.ASCII.GetBytes(upstreamResponse), CancellationToken.None);
        await serverIngressWriter.CompleteAsync();
        await clientIngressWriter.CompleteAsync();

        var clientTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());
        var serverTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());

        var trafficStore = new StubTrafficStore();
        var webSocketStore = new WebSocketStore();
        var eventBus = new StubDomainEventBus();
        var noCaching = new NoCachingRule(new MatchingRule("*", MatchingRuleKind.Wildcard), isEnabled: true, priority: 0);
        var ruleEngine = new RuleEngine(Array.Empty<IRequestPhaseRule>(), new IResponsePhaseRule[] { noCaching });
        var dependencies = BuildDependencies(eventBus, trafficStore, webSocketStore, ruleEngine: ruleEngine);
        var handler = new TransportLayerSecurityInterceptedUpgradeHandler(dependencies);

        var request = BuildUpgradeRequest(pipes, clientTunnelStream, serverTunnelStream);

        await handler.HandleAsync(request, CancellationToken.None);

        var wireBytes = await ReadAvailableAsync(clientEgressReader);
        var wireText = Encoding.ASCII.GetString(wireBytes);

        await Assert.That(wireText.Contains("Cache-Control: no-cache", StringComparison.OrdinalIgnoreCase)).IsTrue();
        await Assert.That(wireText.Contains("max-age=600", StringComparison.OrdinalIgnoreCase)).IsFalse();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
        var storedResponse = trafficStore.GetAll()[0].Response!;
        await Assert.That(storedResponse.Headers.Get("Cache-Control")).IsEqualTo("no-cache, no-store, must-revalidate");
    }

    /// <summary>
    ///     The scripting hook is invoked on the upstream upgrade response, and its
    ///     projection reaches the stored flow and the wire bytes.
    /// </summary>
    [Test]
    public async Task HandleAsync_SuccessfulUpgrade_InvokesScriptingResponseHook()
    {
        var pipes = BuildPipes(
            out var clientIngressWriter,
            out var clientEgressReader,
            out var serverIngressWriter,
            out _);
        var upstreamResponse = "HTTP/1.1 101 Switching Protocols\r\n"
            + "Upgrade: websocket\r\n"
            + "Connection: Upgrade\r\n"
            + "Sec-WebSocket-Accept: dGVzdA==\r\n"
            + "\r\n";
        await serverIngressWriter.WriteAsync(Encoding.ASCII.GetBytes(upstreamResponse), CancellationToken.None);
        await serverIngressWriter.CompleteAsync();
        await clientIngressWriter.CompleteAsync();

        var clientTunnelStream = new DuplexStream(new MemoryStream(BuildCloseFrame()), new MemoryStream());
        var serverTunnelStream = new DuplexStream(new MemoryStream(BuildCloseFrame()), new MemoryStream());

        var trafficStore = new StubTrafficStore();
        var webSocketStore = new WebSocketStore();
        var eventBus = new StubDomainEventBus();
        var scriptingHandler = new StubScriptingHandler
        {
            ResponseTransformer = response => AddHeader(response, "X-Script", "applied"),
        };
        var dependencies = BuildDependencies(eventBus, trafficStore, webSocketStore, scriptingHandler: scriptingHandler);
        var handler = new TransportLayerSecurityInterceptedUpgradeHandler(dependencies);

        var request = BuildUpgradeRequest(pipes, clientTunnelStream, serverTunnelStream);

        await handler.HandleAsync(request, CancellationToken.None);

        var wireBytes = await ReadAvailableAsync(clientEgressReader);
        var wireText = Encoding.ASCII.GetString(wireBytes);

        await Assert.That(scriptingHandler.ResponseInvocationCount).IsEqualTo(1);
        await Assert.That(wireText.Contains("X-Script: applied", StringComparison.Ordinal)).IsTrue();
        await Assert.That(trafficStore.GetAll()[0].Response!.Headers.Get("X-Script")).IsEqualTo("applied");
    }

    /// <summary>
    ///     An aborting response-phase breakpoint decision short-circuits the upgrade: nothing
    ///     is written to the client, the flow is marked Failed, and the traffic store is not
    ///     updated.
    /// </summary>
    [Test]
    public async Task HandleAsync_BreakpointAborts_FailsFlowAndSuppressesClientWrite()
    {
        var pipes = BuildPipes(
            out var clientIngressWriter,
            out var clientEgressReader,
            out var serverIngressWriter,
            out _);
        var upstreamResponse = "HTTP/1.1 101 Switching Protocols\r\n"
            + "Upgrade: websocket\r\n"
            + "Connection: Upgrade\r\n"
            + "Sec-WebSocket-Accept: dGVzdA==\r\n"
            + "\r\n";
        await serverIngressWriter.WriteAsync(Encoding.ASCII.GetBytes(upstreamResponse), CancellationToken.None);
        await serverIngressWriter.CompleteAsync();
        await clientIngressWriter.CompleteAsync();

        var clientTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());
        var serverTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());

        var trafficStore = new StubTrafficStore();
        var webSocketStore = new WebSocketStore();
        var eventBus = new StubDomainEventBus();
        var breakpointHandler = new StubBreakpointHandler
        {
            ResponseDecision = BreakpointDecisions.Abort(),
        };
        var dependencies = BuildDependencies(eventBus, trafficStore, webSocketStore, breakpointHandler: breakpointHandler);
        var handler = new TransportLayerSecurityInterceptedUpgradeHandler(dependencies);

        var request = BuildUpgradeRequest(pipes, clientTunnelStream, serverTunnelStream);

        await handler.HandleAsync(request, CancellationToken.None);

        var wireBytes = await ReadAvailableAsync(clientEgressReader);

        await Assert.That(breakpointHandler.ResponseResolveCount).IsEqualTo(1);
        await Assert.That(wireBytes.Length).IsEqualTo(0);
        await Assert.That(trafficStore.Count).IsEqualTo(0);
        await Assert.That(webSocketStore.Count).IsEqualTo(0);
        await Assert.That(request.Flow.Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>
    ///     A modifying response-phase breakpoint decision replaces the upstream response and
    ///     the modification reaches the wire bytes and the stored flow.
    /// </summary>
    [Test]
    public async Task HandleAsync_BreakpointModifiesResponse_ForwardsModifiedResponse()
    {
        var pipes = BuildPipes(
            out var clientIngressWriter,
            out var clientEgressReader,
            out var serverIngressWriter,
            out _);
        var upstreamResponse = "HTTP/1.1 426 Upgrade Required\r\nContent-Length: 0\r\n\r\n";
        await serverIngressWriter.WriteAsync(Encoding.ASCII.GetBytes(upstreamResponse), CancellationToken.None);
        await serverIngressWriter.CompleteAsync();
        await clientIngressWriter.CompleteAsync();

        var clientTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());
        var serverTunnelStream = new DuplexStream(new MemoryStream(), new MemoryStream());

        var trafficStore = new StubTrafficStore();
        var webSocketStore = new WebSocketStore();
        var eventBus = new StubDomainEventBus();
        var modifiedResponse = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty.Add("X-Modified", "true").Add("Content-Length", "0"),
            ReasonPhrase = "Forbidden",
            StatusCode = 403,
            Version = "HTTP/1.1",
        });
        var breakpointHandler = new StubBreakpointHandler
        {
            ResponseDecision = BreakpointDecisions.ResumeResponse(modifiedResponse),
        };
        var dependencies = BuildDependencies(eventBus, trafficStore, webSocketStore, breakpointHandler: breakpointHandler);
        var handler = new TransportLayerSecurityInterceptedUpgradeHandler(dependencies);

        var request = BuildUpgradeRequest(pipes, clientTunnelStream, serverTunnelStream);

        await handler.HandleAsync(request, CancellationToken.None);

        var wireBytes = await ReadAvailableAsync(clientEgressReader);
        var wireText = Encoding.ASCII.GetString(wireBytes);

        await Assert.That(wireText.StartsWith("HTTP/1.1 403", StringComparison.Ordinal)).IsTrue();
        await Assert.That(wireText.Contains("X-Modified: true", StringComparison.Ordinal)).IsTrue();
        await Assert.That(trafficStore.Count).IsEqualTo(1);
        await Assert.That(trafficStore.GetAll()[0].Response!.StatusCode).IsEqualTo(403);
    }

    private static TransportLayerSecurityInterceptedUpgradeHandlerDependencies BuildDependencies(
        StubDomainEventBus eventBus,
        ITrafficStore trafficStore,
        IWebSocketStore? webSocketStore,
        IRuleEngine? ruleEngine = null,
        IScriptingHandler? scriptingHandler = null,
        IBreakpointHandler? breakpointHandler = null)
    {
        IRuleEngine effectiveRuleEngine;
        if (ruleEngine is not null)
        {
            effectiveRuleEngine = ruleEngine;
        }
        else
        {
            var registry = new RuleRegistry();
            if (scriptingHandler is not null)
            {
                var scriptingRule = new ScriptingRule(scriptingHandler, NullLogger<ScriptingRule>.Instance);
                registry.RegisterAsyncResponsePhaseRule(scriptingRule);
            }
            if (breakpointHandler is not null)
            {
                var breakpointRule = new BreakpointRule(breakpointHandler);
                registry.RegisterAsyncResponsePhaseRule(breakpointRule);
            }
            effectiveRuleEngine = new RuleEngine(registry, NullLogger<RuleEngine>.Instance);
        }
        return new TransportLayerSecurityInterceptedUpgradeHandlerDependencies
        {
            EventBus = eventBus,
            Logger = NullLogger.Instance,
            RuleEngine = effectiveRuleEngine,
            TimeProvider = TimeProvider.System,
            TrafficStore = trafficStore,
            WebSocketStore = webSocketStore,
        };
    }

    private static HypertextTransferProtocolResponseData AddHeader(
        HypertextTransferProtocolResponseData response,
        string name,
        string value)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = response.Body,
            Headers = response.Headers.Add(name, value),
            ReasonPhrase = response.ReasonPhrase,
            StatusCode = response.StatusCode,
            Version = response.Version,
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }

    private static async Task<byte[]> ReadAvailableAsync(PipeReader reader)
    {
        var collected = new List<byte>();
        while (true)
        {
            if (!reader.TryRead(out var result))
            {
                using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                try
                {
                    result = await reader.ReadAsync(cancellationSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            foreach (var segment in result.Buffer)
            {
                collected.AddRange(segment.ToArray());
            }

            reader.AdvanceTo(result.Buffer.End);
            if (result.IsCompleted)
            {
                break;
            }
        }

        return collected.ToArray();
    }

    private static TransportLayerSecurityInterceptionPipes BuildPipes(
        out PipeWriter clientIngressWriter,
        out PipeReader clientEgressReader,
        out PipeWriter serverIngressWriter,
        out PipeReader serverEgressReader)
    {
        var clientIngress = new Pipe();
        var clientEgress = new Pipe();
        var serverIngress = new Pipe();
        var serverEgress = new Pipe();
        clientIngressWriter = clientIngress.Writer;
        clientEgressReader = clientEgress.Reader;
        serverIngressWriter = serverIngress.Writer;
        serverEgressReader = serverEgress.Reader;
        return new TransportLayerSecurityInterceptionPipes(
            clientIngress.Reader,
            clientEgress.Writer,
            serverIngress.Reader,
            serverEgress.Writer);
    }

    private static TransportLayerSecurityInterceptedUpgradeRequest BuildUpgradeRequest(
        TransportLayerSecurityInterceptionPipes pipes,
        Stream clientTunnelStream,
        Stream serverTunnelStream)
    {
        var headers = HeaderCollection.Empty
            .Add("Host", "example.com")
            .Add("Upgrade", "websocket")
            .Add("Connection", "Upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13");
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("/chat", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        var requestData = new HypertextTransferProtocolRequestData(requestParameters);
        var requestExchange = new HypertextTransferProtocolProxyRequestExchange(
            ReadOnlyMemory<byte>.Empty,
            Encoding.ASCII.GetBytes(
                "GET /chat HTTP/1.1\r\nHost: example.com\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n\r\n"),
            requestData);
        var loopContext = new TransportLayerSecurityInterceptedLoopContext
        {
            ClientSecureStream = clientTunnelStream,
            Connection = new StubProxyConnection(),
            Pipes = pipes,
            ServerSecureStream = serverTunnelStream,
        };
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        flow.SetRequest(requestData);
        return new TransportLayerSecurityInterceptedUpgradeRequest
        {
            Context = loopContext,
            EffectiveRequest = requestData,
            Flow = flow,
            RequestExchange = requestExchange,
        };
    }

    private static byte[] BuildCloseFrame()
    {
        return new byte[] { 0x88, 0x00 };
    }

    private sealed class DuplexStream : Stream
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;

        public DuplexStream(Stream readStream, Stream writeStream)
        {
            _readStream = readStream;
            _writeStream = writeStream;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            _writeStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _writeStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _readStream.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _readStream.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _writeStream.Write(buffer, offset, count);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _writeStream.WriteAsync(buffer, cancellationToken);
        }
    }
}
