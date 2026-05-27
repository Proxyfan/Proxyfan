using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Behavioral tests for <see cref="TransportLayerSecurityInterceptorHandler" /> covering
///     the request-handling pipeline (tunnel mode, intercept mode, and error paths).
/// </summary>
[NotInParallel]
public sealed class TransportLayerSecurityInterceptorHandlerHandleAsyncTests
{
    private const int UnreachablePort = 1;

    /// <summary>
    ///     When proxying is disabled and the upstream host is unreachable, the handler must respond
    ///     with HTTP 502 Bad Gateway from the tunnel error path.
    /// </summary>
    [Test]
    public async Task HandleAsync_TunnelModeWithUnreachableHost_WritesBadGatewayResponse()
    {
        var handler = CreateHandler(proxyingEnabled: false);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes($"CONNECT 127.0.0.1:{UnreachablePort} HTTP/1.1\r\nHost: 127.0.0.1:{UnreachablePort}\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(output);

        await Assert.That(outputText.StartsWith("HTTP/1.1 502", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     When proxying is enabled and the upstream host is unreachable, the handler must respond
    ///     with HTTP 502 Bad Gateway from the intercept error path.
    /// </summary>
    [Test]
    public async Task HandleAsync_InterceptModeWithUnreachableHost_WritesBadGatewayResponse()
    {
        var handler = CreateHandler(proxyingEnabled: true);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes($"CONNECT 127.0.0.1:{UnreachablePort} HTTP/1.1\r\nHost: 127.0.0.1:{UnreachablePort}\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(output);

        await Assert.That(outputText.StartsWith("HTTP/1.1 502", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     When the input stream closes before any data arrives, the handler must return without
    ///     throwing and write a 502 Bad Gateway response (no headers received).
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyInput_WritesBadGatewayResponse()
    {
        var handler = CreateHandler(proxyingEnabled: false);
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(output);

        await Assert.That(outputText.StartsWith("HTTP/1.1 502", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     When the CONNECT request is malformed and cannot be parsed, the handler must respond
    ///     with HTTP 502 Bad Gateway and stop processing.
    /// </summary>
    [Test]
    public async Task HandleAsync_MalformedConnectRequest_WritesBadGatewayResponse()
    {
        var handler = CreateHandler(proxyingEnabled: false);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes("CONNECT not-a-valid-target HTTP/1.1\r\nHost: x\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(output);

        await Assert.That(outputText.StartsWith("HTTP/1.1 502", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     When the CONNECT request never finishes (no blank line), the handler must return
    ///     with no response written.
    /// </summary>
    [Test]
    public async Task HandleAsync_IncompleteHeaders_WritesNothing()
    {
        var handler = CreateHandler(proxyingEnabled: false);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes("CONNECT example.com:443 HTTP/1.1\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(output);

        await Assert.That(outputText.StartsWith("HTTP/1.1 502", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     When proxying is disabled and the upstream accepts the connection, the handler must
    ///     respond with HTTP 200 Connection Established to confirm the tunnel is open.
    /// </summary>
    [Test]
    public async Task HandleAsync_TunnelModeWithReachableHost_WritesConnectionEstablished()
    {
        using var listener = StartTcpListener();
        var endPoint = (IPEndPoint)listener.LocalEndpoint;
        var acceptTask = AcceptAndIgnoreAsync(listener);

        var handler = CreateHandler(proxyingEnabled: false);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes($"CONNECT 127.0.0.1:{endPoint.Port} HTTP/1.1\r\nHost: 127.0.0.1:{endPoint.Port}\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await acceptTask;
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(output);

        await Assert.That(outputText.StartsWith("HTTP/1.1 200", StringComparison.Ordinal)).IsTrue();
    }

    private static TransportLayerSecurityInterceptorHandler CreateHandler(bool proxyingEnabled)
    {
        var proxyingList = new ServerNameIndicationProxyingList(isEnabled: proxyingEnabled);
        var context = new TransportLayerSecurityInterceptionContext(new MutableCertificateAuthorityProvider(new StubCertificateGenerator()), proxyingList);
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var handler = new TransportLayerSecurityInterceptorHandler(new TransportLayerSecurityInterceptorHandlerDependencies
        {
            Context = context,
            TrafficStore = trafficStore,
            EventBus = eventBus,
            Logger = NullLogger<TransportLayerSecurityInterceptorHandler>.Instance,
        });
        return handler;
    }

    private static TcpListener StartTcpListener()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static async Task AcceptAndIgnoreAsync(TcpListener listener)
    {
        try
        {
            using var client = await listener.AcceptTcpClientAsync();
        }
        catch (SocketException)
        {
            // The client may close before accept completes; ignore.
        }
        catch (ObjectDisposedException)
        {
            // The listener may be disposed; ignore.
        }
    }
}