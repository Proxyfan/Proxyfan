using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Scripting;
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
///     Coverage tests that exercise the remaining branches inside
///     <see cref="HypertextTransferProtocolProxyHandler" /> that were not reached by the
///     existing rule, breakpoint, upstream, and dependency tests: the scripting hook
///     null/non-null/throwing paths, HTTP/1.0 keep-alive, missing Content-Length, the
///     breakpoint-modifies-{request,response} fallback fork, and unknown-remote-endpoint
///     traffic flow creation.
/// </summary>
[NotInParallel]
public sealed class HypertextTransferProtocolProxyHandlerCoverageTests
{
    /// <summary>
    ///     A non-null scripting handler runs the request-phase hook with each exchange.
    /// </summary>
    [Test]
    public async Task HandleAsync_ScriptingHandlerConfigured_InvokesRequestAndResponseHooks()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var scripting = new StubScriptingHandler();
        var handler = CreateHandler(scriptingHandler: scripting);
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(scripting.RequestInvocationCount).IsEqualTo(1);
        await Assert.That(scripting.ResponseInvocationCount).IsEqualTo(1);
    }

    /// <summary>
    ///     When the scripting request-phase hook throws a non-cancellation exception, the
    ///     exception is swallowed and traffic continues unmodified.
    /// </summary>
    [Test]
    public async Task HandleAsync_ScriptingRequestHookThrows_FallsThroughToUnmodifiedRequest()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var scripting = new StubScriptingHandler { RequestException = new InvalidOperationException("boom") };
        var handler = CreateHandler(scriptingHandler: scripting);
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(scripting.RequestInvocationCount).IsEqualTo(1);
        await Assert.That(scripting.ResponseInvocationCount).IsEqualTo(1);
    }

    /// <summary>
    ///     When the scripting response-phase hook throws a non-cancellation exception, the
    ///     exception is swallowed and the original response is forwarded.
    /// </summary>
    [Test]
    public async Task HandleAsync_ScriptingResponseHookThrows_FallsThroughToUnmodifiedResponse()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var scripting = new StubScriptingHandler { ResponseException = new InvalidOperationException("boom") };
        var handler = CreateHandler(scriptingHandler: scripting);
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(scripting.ResponseInvocationCount).IsEqualTo(1);
    }

    /// <summary>
    ///     When the scripting request-phase hook returns a <see cref="ScriptError" /> failure
    ///     result, traffic continues unmodified instead of being aborted.
    /// </summary>
    [Test]
    public async Task HandleAsync_ScriptingRequestHookFailureResult_FallsThroughToUnmodifiedRequest()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var scripting = new StubScriptingHandler { RequestError = new ScriptError("SCRIPT_REQUEST_FAILED", "boom") };
        var handler = CreateHandler(scriptingHandler: scripting);
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(scripting.RequestInvocationCount).IsEqualTo(1);
        await Assert.That(scripting.ResponseInvocationCount).IsEqualTo(1);
    }

    /// <summary>
    ///     When the scripting response-phase hook returns a <see cref="ScriptError" /> failure
    ///     result, the original response is forwarded instead of being aborted.
    /// </summary>
    [Test]
    public async Task HandleAsync_ScriptingResponseHookFailureResult_FallsThroughToUnmodifiedResponse()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var scripting = new StubScriptingHandler { ResponseError = new ScriptError("SCRIPT_RESPONSE_FAILED", "boom") };
        var handler = CreateHandler(scriptingHandler: scripting);
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(scripting.ResponseInvocationCount).IsEqualTo(1);
    }

    /// <summary>
    ///     A breakpoint that returns a modified request causes the modified request to be
    ///     forwarded (covers the `??` left-hand branch in ProcessSingleExchangeAsync).
    /// </summary>
    [Test]
    public async Task HandleAsync_BreakpointReturnsModifiedRequest_ForwardsModifiedRequest()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var trafficStore = new StubTrafficStore();
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);
        var breakpoint = new StubBreakpointHandler();
        breakpoint.RequestDecision = BreakpointDecisions.ResumeRequest(BuildSimpleRequest(upstreamPort, "PATCHED"));
        var handler = CreateHandler(breakpointHandler: breakpoint, trafficStore: trafficStore);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(breakpoint.RequestResolveCount).IsEqualTo(1);
    }

    /// <summary>
    ///     A response-phase breakpoint that returns a modified response causes the modified
    ///     response to be returned (covers the `??` left-hand branch in ProcessResponsePhaseAsync).
    /// </summary>
    [Test]
    public async Task HandleAsync_BreakpointReturnsModifiedResponse_WritesModifiedResponse()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);
        var breakpoint = new StubBreakpointHandler();
        var headers = HeaderCollection.Empty.Add("Content-Length", "5");
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.ASCII.GetBytes("HELLO"),
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 201,
            Version = "HTTP/1.1",
        };
        breakpoint.ResponseDecision = BreakpointDecisions.ResumeResponse(new HypertextTransferProtocolResponseData(responseParameters));
        var handler = CreateHandler(breakpointHandler: breakpoint);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(breakpoint.ResponseResolveCount).IsEqualTo(1);
    }

    /// <summary>
    ///     An HTTP/1.0 request causes the keep-alive check to refuse continuation (covers the
    ///     true branch of the HTTP/1.0 string-equals check).
    /// </summary>
    [Test]
    public async Task HandleAsync_HypertextTransferProtocolVersion10Request_DoesNotKeepConnectionAlive()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes(
            $"GET /api HTTP/1.0\r\nHost: 127.0.0.1:{upstreamPort}\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();
        var handler = CreateHandler();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();
        await connection.Transport.Output.CompleteAsync();

        var output = await connection.ReadAllOutputAsync();
        await Assert.That(output.Length).IsGreaterThan(0);
    }

    /// <summary>
    ///     A response without Content-Length causes the keep-alive check to refuse continuation
    ///     (covers the true branch of the Content-Length header presence check).
    /// </summary>
    [Test]
    public async Task HandleAsync_ResponseWithoutContentLength_DoesNotKeepConnectionAlive()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nConnection: close\r\n\r\n");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var connection = new StubFullDuplexProxyConnection();
        await WriteSimpleRequestAsync(connection, upstreamPort);
        var handler = CreateHandler();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();
        await connection.Transport.Output.CompleteAsync();

        var output = await connection.ReadAllOutputAsync();
        await Assert.That(output.Length).IsGreaterThan(0);
    }

    /// <summary>
    ///     A proxy connection with a null RemoteEndPoint causes the captured TrafficFlow to
    ///     fall back to the literal "unknown" client endpoint (covers the right-hand side of
    ///     `RemoteEndPoint?.ToString() ?? "unknown"`).
    /// </summary>
    [Test]
    public async Task HandleAsync_ConnectionWithNullRemoteEndPoint_RecordsUnknownClientEndPoint()
    {
        using var upstream = StartHttpServer("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        var upstreamPort = ((IPEndPoint)upstream.Listener.LocalEndpoint).Port;
        var trafficStore = new StubTrafficStore();
        var connection = new NoEndPointStubProxyConnection();
        await connection.WriteRequestAsync($"GET /api HTTP/1.1\r\nHost: 127.0.0.1:{upstreamPort}\r\nConnection: close\r\n\r\n");
        var handler = CreateHandler(trafficStore: trafficStore);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();

        await Assert.That(trafficStore.Count).IsEqualTo(1);
        await Assert.That(trafficStore.AddedFlows[0].ClientEndPoint).IsEqualTo("unknown");
    }

    /// <summary>
    ///     A request with an empty host segment (":80") triggers the empty-host branch of
    ///     ParseHostEndpoint and the handler closes the connection without writing a
    ///     successful response.
    /// </summary>
    [Test]
    public async Task HandleAsync_HostHeaderWithEmptyHost_ClosesConnectionGracefully()
    {
        var trafficStore = new StubTrafficStore();
        var handler = CreateHandler(trafficStore: trafficStore);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: :80\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();

        await Assert.That(trafficStore.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     A request whose Host header has a non-numeric port triggers the int.TryParse failure
    ///     branch of ParseHostEndpoint and the handler closes the connection gracefully.
    /// </summary>
    [Test]
    public async Task HandleAsync_HostHeaderWithNonNumericPort_ClosesConnectionGracefully()
    {
        var trafficStore = new StubTrafficStore();
        var handler = CreateHandler(trafficStore: trafficStore);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com:abc\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();

        await Assert.That(trafficStore.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     A request whose Host header has a port outside [1, 65535] triggers the range-check
    ///     branch of ParseHostEndpoint and the handler closes the connection gracefully.
    /// </summary>
    [Test]
    public async Task HandleAsync_HostHeaderWithOutOfRangePort_ClosesConnectionGracefully()
    {
        var trafficStore = new StubTrafficStore();
        var handler = CreateHandler(trafficStore: trafficStore);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com:99999\r\nConnection: close\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();

        await Assert.That(trafficStore.Count).IsEqualTo(0);
    }

    private static HypertextTransferProtocolRequestData BuildSimpleRequest(int upstreamPort, string method)
    {
        var uri = new Uri($"http://127.0.0.1:{upstreamPort}/api");
        var headers = HeaderCollection.Empty
            .Add("Host", $"127.0.0.1:{upstreamPort}")
            .Add("Connection", "close");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = method,
            RequestUri = uri,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolProxyHandler CreateHandler(
        IBreakpointHandler? breakpointHandler = null,
        IScriptingHandler? scriptingHandler = null,
        ITrafficStore? trafficStore = null)
    {
        var registry = new RuleRegistry();
        if (scriptingHandler is not null)
        {
            var scriptingRule = new ScriptingRule(scriptingHandler, NullLogger<ScriptingRule>.Instance);
            registry.RegisterAsyncRequestPhaseRule(scriptingRule);
            registry.RegisterAsyncResponsePhaseRule(scriptingRule);
        }
        if (breakpointHandler is not null)
        {
            var breakpointRule = new BreakpointRule(breakpointHandler);
            registry.RegisterAsyncRequestPhaseRule(breakpointRule);
            registry.RegisterAsyncResponsePhaseRule(breakpointRule);
        }
        var ruleEngine = new RuleEngine(registry, NullLogger<RuleEngine>.Instance);
        var handler = new HypertextTransferProtocolProxyHandler(new HypertextTransferProtocolProxyHandlerDependencies
        {
            TrafficStore = trafficStore ?? new StubTrafficStore(),
            EventBus = new StubDomainEventBus(),
            RuleEngine = ruleEngine,
            Logger = NullLogger<HypertextTransferProtocolProxyHandler>.Instance,
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
            _ = await stream.ReadAsync(buffer);
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

    private sealed class NoEndPointStubProxyConnection : Proxyfan.Domain.Proxy.IProxyConnection
    {
        private readonly System.IO.Pipelines.Pipe _inputPipe;
        private readonly System.IO.Pipelines.Pipe _outputPipe;

        public EndPoint RemoteEndPoint { get; } = null!;

        public System.IO.Pipelines.IDuplexPipe Transport { get; }

        public NoEndPointStubProxyConnection()
        {
            _inputPipe = new System.IO.Pipelines.Pipe();
            _outputPipe = new System.IO.Pipelines.Pipe();
            Transport = new DuplexPipe(_inputPipe.Reader, _outputPipe.Writer);
        }

        public async Task WriteRequestAsync(string request)
        {
            var bytes = Encoding.ASCII.GetBytes(request);
            await _inputPipe.Writer.WriteAsync(bytes);
            await _inputPipe.Writer.CompleteAsync();
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        private sealed class DuplexPipe : System.IO.Pipelines.IDuplexPipe
        {
            public System.IO.Pipelines.PipeReader Input { get; }

            public System.IO.Pipelines.PipeWriter Output { get; }

            public DuplexPipe(System.IO.Pipelines.PipeReader input, System.IO.Pipelines.PipeWriter output)
            {
                Input = input;
                Output = output;
            }
        }
    }
}
