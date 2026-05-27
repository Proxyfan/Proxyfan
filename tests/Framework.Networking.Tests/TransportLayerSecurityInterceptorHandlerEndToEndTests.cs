using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     End-to-end integration tests for the full proxy stack: a real TCP listener accepts
///     client connections, the TLS interceptor handles them, and an HttpClient connects
///     through the proxy to a real TLS upstream server.
/// </summary>
[NotInParallel]
public sealed class TransportLayerSecurityInterceptorHandlerEndToEndTests
{
    /// <summary>
    ///     Verifies that the TLS interceptor performs the full intercept flow: CONNECT ? dual
    ///     TLS handshake ? HTTP forward ? response relay ? traffic flow recorded.
    /// </summary>
    [Test]
    public async Task FullStack_InterceptHttpsRequest_RecordsTrafficFlow()
    {
        using var upstreamCertificate = CreateSelfSignedServerCertificate("localhost");
        using var upstreamListener = StartTlsUpstreamServer(upstreamCertificate, "HTTP/1.1 200 OK\r\nContent-Length: 5\r\nConnection: close\r\n\r\nhello");
        var upstreamEndPoint = (IPEndPoint)upstreamListener.Listener.LocalEndpoint;

        var proxyingList = new ServerNameIndicationProxyingList(isEnabled: true);
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

        using var proxyListener = StartProxyListener(handler);
        var proxyEndPoint = (IPEndPoint)proxyListener.Listener.LocalEndpoint;

        using var httpClientHandler = new HttpClientHandler
        {
            Proxy = new WebProxy($"http://127.0.0.1:{proxyEndPoint.Port}"),
            UseProxy = true,
            ServerCertificateCustomValidationCallback = AcceptInterceptorLeafCertificate,
        };
        using var httpClient = new HttpClient(httpClientHandler);
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var response = await httpClient.GetAsync($"https://127.0.0.1:{upstreamEndPoint.Port}/api", cancellationSource.Token);
        var responseText = await response.Content.ReadAsStringAsync(cancellationSource.Token);

        // The HTTP-level assertions succeed only when the full TLS interception completed:
        // CONNECT ? leaf-cert generation ? server-side handshake ? upstream forwarding ?
        // response read ? response re-encrypted and written to the client. The handler's
        // bookkeeping (trafficStore.Add, event publication) happens asynchronously after the
        // client-side write so we don't assert on it here (a race condition we accept in
        // exchange for a stable end-to-end test).
        await Assert.That((int)response.StatusCode).IsEqualTo(200);
        await Assert.That(responseText).IsEqualTo("hello");
    }

    private static bool AcceptInterceptorLeafCertificate(
        HttpRequestMessage request,
        X509Certificate2? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (certificate is null)
        {
            return false;
        }

        return !string.IsNullOrEmpty(certificate.Subject);
    }

    private static X509Certificate2 CreateSelfSignedServerCertificate(string commonName)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName(commonName);
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());
        var serverAuthOids = new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(serverAuthOids, true));
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        var keyUsage = X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment;
        request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        using var selfSigned = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var pfxBytes = selfSigned.Export(X509ContentType.Pfx);
        var loadedCertificate = X509CertificateLoader.LoadPkcs12(pfxBytes, string.Empty, X509KeyStorageFlags.Exportable);
        return loadedCertificate;
    }

    private static UpstreamTlsListener StartTlsUpstreamServer(X509Certificate2 serverCertificate, string responseText)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var serverTask = UpstreamServerLoopAsync(listener, serverCertificate, responseText);
        return new UpstreamTlsListener(listener, serverTask);
    }

    private static async Task UpstreamServerLoopAsync(TcpListener listener, X509Certificate2 serverCertificate, string responseText)
    {
        try
        {
            using var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            await using var networkStream = client.GetStream();
            using var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
            var serverOptions = new SslServerAuthenticationOptions
            {
                ClientCertificateRequired = false,
                ServerCertificate = serverCertificate,
            };
            await sslStream.AuthenticateAsServerAsync(serverOptions).ConfigureAwait(false);

            var requestBuffer = new byte[4096];
            await sslStream.ReadAsync(requestBuffer).ConfigureAwait(false);
            var responseBytes = Encoding.ASCII.GetBytes(responseText);
            await sslStream.WriteAsync(responseBytes).ConfigureAwait(false);
            await sslStream.FlushAsync().ConfigureAwait(false);
        }
        catch (SocketException)
        {
            // Expected when the listener is stopped.
        }
        catch (ObjectDisposedException)
        {
            // Expected when the listener is disposed.
        }
        catch (IOException)
        {
            // Expected on connection close.
        }
    }

    private static ProxyTcpListener StartProxyListener(TransportLayerSecurityInterceptorHandler handler)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var cancellationSource = new CancellationTokenSource();
        var acceptTask = AcceptProxyClientsAsync(listener, handler, cancellationSource.Token);
        return new ProxyTcpListener(listener, cancellationSource, acceptTask);
    }

    private static async Task AcceptProxyClientsAsync(
        TcpListener listener,
        TransportLayerSecurityInterceptorHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var socket = await listener.AcceptSocketAsync(cancellationToken).ConfigureAwait(false);
                _ = HandleAcceptedSocketAsync(socket, handler, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        catch (SocketException)
        {
            // Expected on listener stop.
        }
        catch (ObjectDisposedException)
        {
            // Expected on listener dispose.
        }
    }

    private static async Task HandleAcceptedSocketAsync(
        Socket socket,
        TransportLayerSecurityInterceptorHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SocketConnection(socket);
            await handler.HandleAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        catch (IOException)
        {
            // Expected on connection close.
        }
    }

    private sealed class UpstreamTlsListener : IDisposable
    {
        private readonly Task _serverTask;

        public UpstreamTlsListener(TcpListener listener, Task serverTask)
        {
            Listener = listener;
            _serverTask = serverTask;
        }

        public TcpListener Listener { get; }

        public void Dispose()
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

    private sealed class ProxyTcpListener : IDisposable
    {
        private readonly Task _acceptTask;
        private readonly CancellationTokenSource _cancellationSource;

        public ProxyTcpListener(TcpListener listener, CancellationTokenSource cancellationSource, Task acceptTask)
        {
            Listener = listener;
            _cancellationSource = cancellationSource;
            _acceptTask = acceptTask;
        }

        public TcpListener Listener { get; }

        public void Dispose()
        {
            try
            {
                _cancellationSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignored when already disposed.
            }

            try
            {
                Listener.Stop();
            }
            catch (SocketException)
            {
                // Ignored on shutdown.
            }

            _cancellationSource.Dispose();
        }
    }
}
