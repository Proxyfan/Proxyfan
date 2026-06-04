using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     End-to-end integration tests for <see cref="ReverseProxyHypertextTransferProtocolHandler" />.
///     Bind a real <see cref="HttpListener" /> backend on a loopback port, bind a real
///     <see cref="ReverseProxyRouteListener" /> with the handler installed on a different
///     loopback port, send HTTP requests through the proxy with <see cref="HttpClient" />,
///     and assert that responses round-trip, the host header is rewritten, and captured
///     flows surface in the shared <see cref="ITrafficStore" />.
/// </summary>
[NotInParallel]
public sealed class ReverseProxyHypertextTransferProtocolHandlerEndToEndTests
{
    /// <summary>
    ///     A GET sent to the reverse proxy returns the backend's response body and surfaces a
    ///     traffic flow with the rewritten Host header.
    /// </summary>
    [Test]
    public async Task HandleAsync_GetRequest_RoundTripsResponseAndCapturesFlow()
    {
        // Keep the backend probe alive until RunRawBackendAsync binds to minimise the race window.
        var backendProbe = new TcpListener(IPAddress.Loopback, 0);
        backendProbe.Start();
        var backendPort = ((IPEndPoint)backendProbe.LocalEndpoint).Port;
        backendProbe.Stop(); // Release immediately before the backend binds.

        using var backendCancellation = new CancellationTokenSource();
        var capturedHost = new TaskCompletionSource<string>();
        var backendTask = RunRawBackendAsync(backendPort, "hello-backend", capturedHost, backendCancellation.Token);

        var trafficStore = new TrafficStore();
        var eventBus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var ruleEngine = new RuleEngine([], []);
        var handler = CreateHandler(eventBus, ruleEngine, trafficStore);

        // BindRouteListenerAsync keeps the listen-port probe alive while constructing the
        // listener, then releases it just before StartAsync (bind-and-pass + retry).
        var (listener, listenPort) = await BindRouteListenerAsync(
            port => CreateListener(port, backendPort, handler));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listenPort);
            var stream = client.GetStream();
            var request = $"GET /hello HTTP/1.1\r\nHost: 127.0.0.1:{listenPort}\r\nConnection: close\r\n\r\n";
            var requestBytes = Encoding.ASCII.GetBytes(request);
            await stream.WriteAsync(requestBytes, CancellationToken.None);
            await stream.FlushAsync(CancellationToken.None);

            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: false);
            var rawResponse = await reader.ReadToEndAsync(CancellationToken.None);

            await Assert.That(rawResponse).Contains("HTTP/1.1 200");
            await Assert.That(rawResponse).Contains("hello-backend");

            var seenHost = await capturedHost.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.That(seenHost).IsEqualTo($"127.0.0.1:{backendPort}");

            var capturedFlow = await PollForFirstFlowAsync(trafficStore, TimeSpan.FromSeconds(5));
            await Assert.That(capturedFlow).IsNotNull();
            await Assert.That(capturedFlow!.Request).IsNotNull();
            await Assert.That(capturedFlow.Request!.Headers.Get("Host"))
                .IsEqualTo($"127.0.0.1:{listenPort}");
            await Assert.That(capturedFlow.Response).IsNotNull();
            await Assert.That(capturedFlow.Response!.StatusCode).IsEqualTo(200);
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
            await backendCancellation.CancelAsync();
            try
            {
                await backendTask;
            }
            catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException or IOException)
            {
                _ = ex;
            }
        }
    }

    /// <summary>
    ///     When a Block List rule matches the incoming request, the handler returns 403 and
    ///     never opens a connection to the backend; the flow is still recorded with the
    ///     blocked response.
    /// </summary>
    [Test]
    public async Task HandleAsync_BlockedRequest_Returns403AndCapturesBlockedFlow()
    {
        // backendPort is intentionally never bound (block list fires before forwarding).
        var backendProbe = new TcpListener(IPAddress.Loopback, 0);
        backendProbe.Start();
        var backendPort = ((IPEndPoint)backendProbe.LocalEndpoint).Port;
        backendProbe.Stop();

        var trafficStore = new TrafficStore();
        var eventBus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var matcher = new MatchingRule("/blocked", MatchingRuleKind.Exact);
        var ruleEngine = new RuleEngine([new BlockListRule([matcher], isEnabled: true, priority: 0)], []);
        var handler = CreateHandler(eventBus, ruleEngine, trafficStore);

        var (listener, listenPort) = await BindRouteListenerAsync(
            port => CreateListener(port, backendPort, handler));

        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{listenPort}/") };

            var response = await httpClient.GetAsync("/blocked");

            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

            var capturedFlow = await PollForFirstFlowAsync(trafficStore, TimeSpan.FromSeconds(5));
            await Assert.That(capturedFlow).IsNotNull();
            await Assert.That(capturedFlow!.Response).IsNotNull();
            await Assert.That(capturedFlow.Response!.StatusCode).IsEqualTo(403);
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
        }
    }

    /// <summary>
    ///     Non-HTTP traffic on the reverse-proxy listener still falls through to raw TCP
    ///     bidirectional pumping, even with an HTTP handler installed.
    /// </summary>
    [Test]
    public async Task HandleAsync_NonHttpTraffic_FallsThroughToRawForwarding()
    {
        var backendProbe = new TcpListener(IPAddress.Loopback, 0);
        backendProbe.Start();
        var backendPort = ((IPEndPoint)backendProbe.LocalEndpoint).Port;
        backendProbe.Stop();

        using var echoCancellation = new CancellationTokenSource();
        var echoTask = RunEchoServerAsync(backendPort, echoCancellation.Token);

        var trafficStore = new TrafficStore();
        var eventBus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var ruleEngine = new RuleEngine([], []);
        var handler = CreateHandler(eventBus, ruleEngine, trafficStore);

        var (listener, listenPort) = await BindRouteListenerAsync(
            port => CreateListener(port, backendPort, handler));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listenPort);
            using var stream = client.GetStream();
            var payload = Encoding.ASCII.GetBytes("PING");
            await stream.WriteAsync(payload);

            var buffer = new byte[payload.Length];
            using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read;
                try
                {
                    read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), readCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            await Assert.That(totalRead).IsEqualTo(payload.Length);
            await Assert.That(Encoding.ASCII.GetString(buffer)).IsEqualTo("PING");
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
            await echoCancellation.CancelAsync();
            try
            {
                await echoTask;
            }
            catch (OperationCanceledException ex)
            {
                _ = ex;
            }
        }
    }

    /// <summary>
    ///     When the backend port is closed, the forwarder fails and the handler completes the
    ///     flow as failed and publishes a <see cref="TrafficFlowCompleted" /> with that status,
    ///     then exits the per-connection loop.
    /// </summary>
    [Test]
    public async Task HandleAsync_BackendUnreachable_FailsFlowAndPublishesCompletedEvent()
    {
        // backendPort is intentionally never bound (testing unreachable backend behavior).
        var backendProbe = new TcpListener(IPAddress.Loopback, 0);
        backendProbe.Start();
        var backendPort = ((IPEndPoint)backendProbe.LocalEndpoint).Port;
        backendProbe.Stop();

        var trafficStore = new TrafficStore();
        var eventBus = new RecordingDomainEventBus();
        var ruleEngine = new RuleEngine([], []);
        var handler = CreateHandler(eventBus, ruleEngine, trafficStore);

        var (listener, listenPort) = await BindRouteListenerAsync(
            port => CreateListener(port, backendPort, handler));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listenPort);
            var stream = client.GetStream();
            var request = $"GET /unreachable HTTP/1.1\r\nHost: 127.0.0.1:{listenPort}\r\nConnection: close\r\n\r\n";
            var requestBytes = Encoding.ASCII.GetBytes(request);
            await stream.WriteAsync(requestBytes, CancellationToken.None);
            await stream.FlushAsync(CancellationToken.None);

            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: false);
            await reader.ReadToEndAsync(CancellationToken.None);

            var completedEvent = await PollForCompletedAsync(eventBus, TimeSpan.FromSeconds(5));
            await Assert.That(completedEvent).IsNotNull();
            await Assert.That(completedEvent!.Status).IsEqualTo(TrafficFlowStatus.Failed);

            var storedFlows = trafficStore.GetAll();
            await Assert.That(storedFlows.Count).IsGreaterThan(0);
            var storedFlow = storedFlows[0];
            await Assert.That(storedFlow.Status).IsEqualTo(TrafficFlowStatus.Failed);
            await Assert.That(storedFlow.Id).IsEqualTo(completedEvent.TrafficFlowId);
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
        }
    }

    /// <summary>
    ///     An HTTP/1.0 request that succeeds is still served, but the handler does not keep
    ///     the connection open after the response (HasCanKeepClientConnectionAlive returns
    ///     false for HTTP/1.0).
    /// </summary>
    [Test]
    public async Task HandleAsync_Http10Request_ClosesConnectionAfterResponse()
    {
        var backendProbe = new TcpListener(IPAddress.Loopback, 0);
        backendProbe.Start();
        var backendPort = ((IPEndPoint)backendProbe.LocalEndpoint).Port;
        backendProbe.Stop();

        using var backendCancellation = new CancellationTokenSource();
        var capturedHost = new TaskCompletionSource<string>();
        var backendTask = RunRawBackendAsync(backendPort, "ten-body", capturedHost, backendCancellation.Token);

        var trafficStore = new TrafficStore();
        var eventBus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var ruleEngine = new RuleEngine([], []);
        var handler = CreateHandler(eventBus, ruleEngine, trafficStore);

        var (listener, listenPort) = await BindRouteListenerAsync(
            port => CreateListener(port, backendPort, handler));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listenPort);
            var stream = client.GetStream();
            var request = $"GET /ten HTTP/1.0\r\nHost: 127.0.0.1:{listenPort}\r\n\r\n";
            var requestBytes = Encoding.ASCII.GetBytes(request);
            await stream.WriteAsync(requestBytes, CancellationToken.None);
            await stream.FlushAsync(CancellationToken.None);

            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: false);
            var rawResponse = await reader.ReadToEndAsync(CancellationToken.None);

            await Assert.That(rawResponse).Contains("ten-body");
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
            await backendCancellation.CancelAsync();
            try
            {
                await backendTask;
            }
            catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException or IOException)
            {
                _ = ex;
            }
        }
    }

    private static ReverseProxyHypertextTransferProtocolHandler CreateHandler(
        IDomainEventBus eventBus,
        IRuleEngine ruleEngine,
        ITrafficStore trafficStore)
    {
        var dependencies = new ReverseProxyHypertextTransferProtocolHandlerDependencies
        {
            EventBus = eventBus,
            Logger = NullLogger<ReverseProxyHypertextTransferProtocolHandler>.Instance,
            RuleEngine = ruleEngine,
            TimeProvider = TimeProvider.System,
            TrafficStore = trafficStore,
        };
        return new ReverseProxyHypertextTransferProtocolHandler(dependencies);
    }

    private static ReverseProxyRouteListener CreateListener(
        int listenPort,
        int backendPort,
        ReverseProxyHypertextTransferProtocolHandler handler)
    {
        var route = new ReverseProxyRoute(
            "test-route",
            "Test",
            listenPort,
            "127.0.0.1",
            backendPort,
            ReverseProxyTransportLayerSecurityMode.None);
        var listener = new ReverseProxyRouteListener(route, NullLogger<ReverseProxyRouteListener>.Instance, handler);
        return listener;
    }

    /// <summary>
    ///     Starts a <see cref="ReverseProxyRouteListener" /> on a free port using the
    ///     bind-probe-and-retry pattern: a <see cref="TcpListener" /> probe on port 0 holds the
    ///     OS port reservation while the production listener is constructed, is then released
    ///     immediately before <see cref="ReverseProxyRouteListener.StartAsync" />, and the
    ///     whole attempt is retried up to five times on <see cref="ProxyBindException" />.
    /// </summary>
    /// <param name="createListener">
    ///     Factory that produces a <see cref="ReverseProxyRouteListener" /> configured for the
    ///     supplied port argument.
    /// </param>
    /// <returns>The started listener and the port it successfully bound to.</returns>
    private static async Task<(ReverseProxyRouteListener Listener, int ListenPort)> BindRouteListenerAsync(
        Func<int, ReverseProxyRouteListener> createListener)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            // Hold the probe alive while constructing the route/listener so the OS port
            // reservation is continuous up to the moment the production socket binds.
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            var listener = createListener(port);
            probe.Stop(); // Release port; production socket binds next.
            try
            {
                await listener.StartAsync(CancellationToken.None);
                return (listener, port);
            }
            catch (ProxyBindException)
            {
                listener.Dispose();
                if (attempt == 4)
                {
                    throw new InvalidOperationException("Unable to bind a free listen port after 5 attempts.");
                }
            }
        }

        throw new InvalidOperationException("Unable to bind a free listen port after 5 attempts.");
    }

    private static async Task<TrafficFlow?> PollForFirstFlowAsync(TrafficStore store, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var snapshot = store.GetAll();
            if (snapshot.Count > 0)
            {
                return snapshot[0];
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25), TimeProvider.System, CancellationToken.None);
        }

        return null;
    }

    private static async Task<Proxyfan.Domain.Traffic.Events.TrafficFlowCompleted?> PollForCompletedAsync(
        RecordingDomainEventBus bus,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var match = bus.FirstOrDefaultOf<Proxyfan.Domain.Traffic.Events.TrafficFlowCompleted>();
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25), TimeProvider.System, CancellationToken.None);
        }

        return null;
    }

    private sealed class RecordingDomainEventBus : IDomainEventBus
    {
        private readonly System.Collections.Generic.List<IDomainEvent> _events;
        private readonly object _lock;

        public RecordingDomainEventBus()
        {
            _events = [];
            _lock = new object();
        }

        public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            lock (_lock)
            {
                _events.Add(domainEvent);
            }
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler) where TEvent : IDomainEvent
        {
            _ = handler;
            return new NoOpDisposable();
        }

        public TEvent? FirstOrDefaultOf<TEvent>() where TEvent : class, IDomainEvent
        {
            lock (_lock)
            {
                foreach (var domainEvent in _events)
                {
                    if (domainEvent is TEvent typed)
                    {
                        return typed;
                    }
                }

                return null;
            }
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private static async Task RunRawBackendAsync(
        int port,
        string responseBody,
        TaskCompletionSource<string> capturedHost,
        CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = HandleRawBackendRequestAsync(client, responseBody, capturedHost, cancellationToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task HandleRawBackendRequestAsync(
        TcpClient client,
        string responseBody,
        TaskCompletionSource<string> capturedHost,
        CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            {
                using var stream = client.GetStream();
                var buffer = new byte[4096];
                var requestText = new StringBuilder();
                while (!cancellationToken.IsCancellationRequested)
                {
                    int read;
                    try
                    {
                        read = await stream.ReadAsync(buffer, cancellationToken);
                    }
                    catch (IOException)
                    {
                        return;
                    }

                    if (read == 0)
                    {
                        return;
                    }

                    requestText.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    if (requestText.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
                    {
                        break;
                    }
                }

                var hostHeader = ExtractHeaderValue(requestText.ToString(), "Host");
                capturedHost.TrySetResult(hostHeader);

                var bodyBytes = Encoding.ASCII.GetBytes(responseBody);
                var responseHeader = $"HTTP/1.1 200 OK\r\nContent-Length: {bodyBytes.Length}\r\nContent-Type: text/plain\r\nConnection: close\r\n\r\n";
                var responseHeaderBytes = Encoding.ASCII.GetBytes(responseHeader);
                await stream.WriteAsync(responseHeaderBytes, cancellationToken);
                await stream.WriteAsync(bodyBytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
        }
        catch (IOException ex)
        {
            _ = ex;
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
        }
    }

    private static string ExtractHeaderValue(string requestText, string headerName)
    {
        var lines = requestText.Split("\r\n", StringSplitOptions.None);
        var prefix = $"{headerName}:";
        foreach (var line in lines)
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return line[prefix.Length..].Trim();
            }
        }
        return string.Empty;
    }

    private static async Task RunEchoServerAsync(int port, CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = EchoOneAsync(client, cancellationToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task EchoOneAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            {
                using var stream = client.GetStream();
                var buffer = new byte[4096];
                while (!cancellationToken.IsCancellationRequested)
                {
                    int read;
                    try
                    {
                        read = await stream.ReadAsync(buffer, cancellationToken);
                    }
                    catch (IOException)
                    {
                        break;
                    }

                    if (read == 0)
                    {
                        break;
                    }

                    try
                    {
                        await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    }
                    catch (IOException)
                    {
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
        }
    }
}
