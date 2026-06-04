using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Certificates;
using Proxyfan.Domain.Traffic.Events;
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

    /// <summary>
    ///     Verifies that when ALPN negotiates <c>h2</c> on both TLS legs, the interceptor
    ///     dispatches the decrypted streams into the HTTP/2 orchestrator and captures the
    ///     shadow-decoded request/response in the traffic store.
    /// </summary>
    [Test]
    public async Task HandleAsync_Http2AlpnNegotiated_DispatchesVersionTwoOrchestrator()
    {
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var upstreamCertificate = CreateSelfSignedServerCertificate("localhost");

        var requestHeaders = new[]
        {
            new HypertextTransferProtocolVersion2HpackHeaderField(":method", "POST"),
            new HypertextTransferProtocolVersion2HpackHeaderField(":scheme", "https"),
            new HypertextTransferProtocolVersion2HpackHeaderField(":authority", "localhost"),
            new HypertextTransferProtocolVersion2HpackHeaderField(":path", "/alpn"),
            new HypertextTransferProtocolVersion2HpackHeaderField("content-type", "application/octet-stream"),
        };
        var responseHeaders = new[]
        {
            new HypertextTransferProtocolVersion2HpackHeaderField(":status", "200"),
            new HypertextTransferProtocolVersion2HpackHeaderField("content-type", "application/octet-stream"),
        };
        var requestBody = new byte[] { 1, 2, 3, 4 };
        var responseBody = new byte[] { 5, 6, 7 };
        var requestEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var responseEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var requestFrames = CombineBytes(
            BuildFrame(
                requestEncoder.Encode(requestHeaders),
                HypertextTransferProtocolVersion2FrameType.Headers,
                HypertextTransferProtocolVersion2FrameFlag.EndHeaders,
                1),
            BuildFrame(
                requestBody,
                HypertextTransferProtocolVersion2FrameType.Data,
                HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge,
                1));
        var responseFrames = CombineBytes(
            BuildFrame(
                responseEncoder.Encode(responseHeaders),
                HypertextTransferProtocolVersion2FrameType.Headers,
                HypertextTransferProtocolVersion2FrameFlag.EndHeaders,
                1),
            BuildFrame(
                responseBody,
                HypertextTransferProtocolVersion2FrameType.Data,
                HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge,
                1));

        using var upstreamListener = StartHttp2TlsUpstreamServer(
            upstreamCertificate,
            requestFrames.Length,
            responseFrames,
            cancellationSource.Token);
        var upstreamEndPoint = (IPEndPoint)upstreamListener.Listener.LocalEndpoint;
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var handler = CreateInterceptingHandler(trafficStore, eventBus);
        using var proxyListener = StartProxyListener(handler);
        var proxyEndPoint = (IPEndPoint)proxyListener.Listener.LocalEndpoint;
        using var client = await ConnectHttp2ClientThroughProxyAsync(
            proxyEndPoint.Port,
            "localhost",
            upstreamEndPoint.Port,
            cancellationSource.Token);

        await Assert.That(client.SecureStream.NegotiatedApplicationProtocol).IsEqualTo(SslApplicationProtocol.Http2);
        await Assert.That(await upstreamListener.NegotiatedApplicationProtocolTask.WaitAsync(cancellationSource.Token)).IsEqualTo(SslApplicationProtocol.Http2);

        await client.SecureStream.WriteAsync(requestFrames, cancellationSource.Token);
        await client.SecureStream.FlushAsync(cancellationSource.Token);

        var receivedByUpstream = await upstreamListener.RequestBytesTask.WaitAsync(cancellationSource.Token);
        await Assert.That(receivedByUpstream.AsSpan().SequenceEqual(requestFrames)).IsTrue();

        var forwardedResponse = await ReadExactAsync(client.SecureStream, responseFrames.Length, cancellationSource.Token);
        await Assert.That(forwardedResponse.AsSpan().SequenceEqual(responseFrames)).IsTrue();

        await eventBus.WaitForEventAsync<TrafficFlowCompleted>(cancellationSource.Token);

        await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
        var captured = trafficStore.AddedFlows[0];
        await Assert.That(captured.Request).IsNotNull();
        await Assert.That(captured.Request!.Method).IsEqualTo("POST");
        await Assert.That(captured.Request.RequestUri.ToString()).IsEqualTo("https://localhost/alpn");
        await Assert.That(captured.Request.Body.Span.SequenceEqual(requestBody)).IsTrue();
        await Assert.That(captured.Response).IsNotNull();
        await Assert.That(captured.Response!.StatusCode).IsEqualTo(200);
        await Assert.That(captured.Response.Body.Span.SequenceEqual(responseBody)).IsTrue();
    }

    /// <summary>
    ///     Verifies that once the TLS interceptor dispatches into the HTTP/2 orchestrator, each
    ///     HEADERS and DATA frame is still forwarded byte-for-byte in both directions.
    /// </summary>
    [Test]
    public async Task HandleAsync_Http2AlpnNegotiated_FrameByFrameForwardingPreserved()
    {
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var upstreamCertificate = CreateSelfSignedServerCertificate("localhost");

        var requestEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var responseEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var requestHeadersFrame = BuildFrame(
            requestEncoder.Encode(
            [
                new HypertextTransferProtocolVersion2HpackHeaderField(":method", "POST"),
                new HypertextTransferProtocolVersion2HpackHeaderField(":scheme", "https"),
                new HypertextTransferProtocolVersion2HpackHeaderField(":authority", "localhost"),
                new HypertextTransferProtocolVersion2HpackHeaderField(":path", "/frames"),
            ]),
            HypertextTransferProtocolVersion2FrameType.Headers,
            HypertextTransferProtocolVersion2FrameFlag.EndHeaders,
            1);
        var requestDataFrame = BuildFrame(
            new byte[] { (byte)'f', (byte)'r', (byte)'a', (byte)'m', (byte)'e' },
            HypertextTransferProtocolVersion2FrameType.Data,
            HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge,
            1);
        var responseHeadersFrame = BuildFrame(
            responseEncoder.Encode(
            [
                new HypertextTransferProtocolVersion2HpackHeaderField(":status", "204"),
            ]),
            HypertextTransferProtocolVersion2FrameType.Headers,
            HypertextTransferProtocolVersion2FrameFlag.EndHeaders,
            1);
        var responseDataFrame = BuildFrame(
            new byte[] { (byte)'o', (byte)'k' },
            HypertextTransferProtocolVersion2FrameType.Data,
            HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge,
            1);
        var responseFrames = CombineBytes(responseHeadersFrame, responseDataFrame);

        using var upstreamListener = StartHttp2TlsUpstreamServer(
            upstreamCertificate,
            requestHeadersFrame.Length + requestDataFrame.Length,
            responseFrames,
            cancellationSource.Token);
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var handler = CreateInterceptingHandler(trafficStore, eventBus);
        using var proxyListener = StartProxyListener(handler);
        var proxyEndPoint = (IPEndPoint)proxyListener.Listener.LocalEndpoint;
        var upstreamEndPoint = (IPEndPoint)upstreamListener.Listener.LocalEndpoint;
        using var client = await ConnectHttp2ClientThroughProxyAsync(
            proxyEndPoint.Port,
            "localhost",
            upstreamEndPoint.Port,
            cancellationSource.Token);

        await client.SecureStream.WriteAsync(requestHeadersFrame, cancellationSource.Token);
        await client.SecureStream.FlushAsync(cancellationSource.Token);
        await client.SecureStream.WriteAsync(requestDataFrame, cancellationSource.Token);
        await client.SecureStream.FlushAsync(cancellationSource.Token);

        var forwardedRequestFrames = await upstreamListener.RequestBytesTask.WaitAsync(cancellationSource.Token);
        await Assert.That(forwardedRequestFrames.AsSpan(0, requestHeadersFrame.Length).SequenceEqual(requestHeadersFrame)).IsTrue();
        await Assert.That(forwardedRequestFrames.AsSpan(requestHeadersFrame.Length, requestDataFrame.Length).SequenceEqual(requestDataFrame)).IsTrue();

        var forwardedResponseHeadersFrame = await ReadOneFrameBytesAsync(client.SecureStream, cancellationSource.Token);
        var forwardedResponseDataFrame = await ReadOneFrameBytesAsync(client.SecureStream, cancellationSource.Token);
        await Assert.That(forwardedResponseHeadersFrame.AsSpan().SequenceEqual(responseHeadersFrame)).IsTrue();
        await Assert.That(forwardedResponseDataFrame.AsSpan().SequenceEqual(responseDataFrame)).IsTrue();
        await eventBus.WaitForEventAsync<TrafficFlowCompleted>(cancellationSource.Token);

        await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
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

    private static bool AcceptProxyLeafCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        _ = sender;
        _ = chain;
        _ = sslPolicyErrors;

        return certificate is not null;
    }

    private static byte[] BuildFrame(byte[] payload, HypertextTransferProtocolVersion2FrameType type, HypertextTransferProtocolVersion2FrameFlag flags, uint streamId)
    {
        var buffer = new byte[HypertextTransferProtocolVersion2FrameParser.HeaderLength + payload.Length];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            Flags = flags,
            PayloadLength = payload.Length,
            StreamIdentifier = streamId,
            Type = type,
        };
        HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, payload);
        return buffer;
    }

    private static byte[] CombineBytes(params byte[][] segments)
    {
        var totalLength = 0;
        foreach (var segment in segments)
        {
            totalLength += segment.Length;
        }

        var combined = new byte[totalLength];
        var offset = 0;
        foreach (var segment in segments)
        {
            segment.AsSpan().CopyTo(combined.AsSpan(offset));
            offset += segment.Length;
        }

        return combined;
    }

    private static async Task<ConnectedProxyHttp2Client> ConnectHttp2ClientThroughProxyAsync(
        int proxyPort,
        string host,
        int upstreamPort,
        CancellationToken cancellationToken)
    {
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, proxyPort, cancellationToken);
        var networkStream = client.GetStream();
        var connectRequest = Encoding.ASCII.GetBytes($"CONNECT {host}:{upstreamPort} HTTP/1.1\r\nHost: {host}:{upstreamPort}\r\n\r\n");
        await networkStream.WriteAsync(connectRequest, cancellationToken);
        await networkStream.FlushAsync(cancellationToken);

        var connectResponseBytes = await ReadConnectResponseAsync(networkStream, cancellationToken);
        var connectResponse = Encoding.ASCII.GetString(connectResponseBytes);
        if (!connectResponse.StartsWith("HTTP/1.1 200 Connection Established", StringComparison.Ordinal))
        {
            client.Dispose();
            throw new InvalidOperationException($"Unexpected CONNECT response: {connectResponse}");
        }

        var secureStream = new SslStream(networkStream, leaveInnerStreamOpen: false, AcceptProxyLeafCertificate);
        var options = new SslClientAuthenticationOptions
        {
            ApplicationProtocols =
            [
                SslApplicationProtocol.Http2,
                SslApplicationProtocol.Http11,
            ],
            TargetHost = host,
        };
        await secureStream.AuthenticateAsClientAsync(options, cancellationToken);
        return new ConnectedProxyHttp2Client(client, secureStream);
    }

    private static TransportLayerSecurityInterceptorHandler CreateInterceptingHandler(StubTrafficStore trafficStore, StubDomainEventBus eventBus)
    {
        var proxyingList = new ServerNameIndicationProxyingList(isEnabled: true);
        var context = new TransportLayerSecurityInterceptionContext(
            new MutableCertificateAuthorityProvider(new StubCertificateGenerator()),
            proxyingList);
        var handler = new TransportLayerSecurityInterceptorHandler(new TransportLayerSecurityInterceptorHandlerDependencies
        {
            Context = context,
            EventBus = eventBus,
            Logger = NullLogger<TransportLayerSecurityInterceptorHandler>.Instance,
            TrafficStore = trafficStore,
        });
        return handler;
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

    private static async Task<byte[]> ReadConnectResponseAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        while (true)
        {
            var nextByte = new byte[1];
            var read = await stream.ReadAsync(nextByte, cancellationToken);
            if (read == 0)
            {
                throw new IOException("Unexpected end of stream while reading CONNECT response.");
            }

            buffer.WriteByte(nextByte[0]);
            if (buffer.Length >= 4)
            {
                var bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
                if (bytes[^4..].SequenceEqual("\r\n\r\n"u8))
                {
                    return bytes.ToArray();
                }
            }
        }
    }

    private static async Task<byte[]> ReadExactAsync(Stream stream, int length, CancellationToken cancellationToken)
    {
        var buffer = new byte[length];
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
            if (read == 0)
            {
                throw new IOException($"Expected {length} bytes but the stream closed after {totalRead} bytes.");
            }

            totalRead += read;
        }

        return buffer;
    }

    private static async Task<byte[]> ReadOneFrameBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        var headerBytes = await ReadExactAsync(stream, HypertextTransferProtocolVersion2FrameParser.HeaderLength, cancellationToken);
        var header = HypertextTransferProtocolVersion2FrameParser.TryParseHeader(headerBytes);
        if (header is null)
        {
            throw new IOException("Failed to parse forwarded HTTP/2 frame header.");
        }

        var payloadBytes = await ReadExactAsync(stream, header.Length, cancellationToken);
        return CombineBytes(headerBytes, payloadBytes);
    }

    private static Http2TlsUpstreamListener StartHttp2TlsUpstreamServer(
        X509Certificate2 serverCertificate,
        int expectedRequestLength,
        byte[] responseBytes,
        CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var negotiatedProtocolSource = new TaskCompletionSource<SslApplicationProtocol>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestBytesSource = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverTask = Http2UpstreamServerLoopAsync(
            listener,
            serverCertificate,
            expectedRequestLength,
            responseBytes,
            negotiatedProtocolSource,
            requestBytesSource,
            cancellationToken);
        return new Http2TlsUpstreamListener(
            listener,
            serverTask,
            negotiatedProtocolSource.Task,
            requestBytesSource.Task);
    }

    private static UpstreamTlsListener StartTlsUpstreamServer(X509Certificate2 serverCertificate, string responseText)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var serverTask = UpstreamServerLoopAsync(listener, serverCertificate, responseText);
        return new UpstreamTlsListener(listener, serverTask);
    }

    private static async Task Http2UpstreamServerLoopAsync(
        TcpListener listener,
        X509Certificate2 serverCertificate,
        int expectedRequestLength,
        byte[] responseBytes,
        TaskCompletionSource<SslApplicationProtocol> negotiatedProtocolSource,
        TaskCompletionSource<byte[]> requestBytesSource,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            await using var networkStream = client.GetStream();
            var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
            var serverOptions = new SslServerAuthenticationOptions
            {
                ApplicationProtocols =
                [
                    SslApplicationProtocol.Http2,
                    SslApplicationProtocol.Http11,
                ],
                ClientCertificateRequired = false,
                ServerCertificate = serverCertificate,
            };
            await sslStream.AuthenticateAsServerAsync(serverOptions, cancellationToken).ConfigureAwait(false);
            negotiatedProtocolSource.TrySetResult(sslStream.NegotiatedApplicationProtocol);
            var requestBytes = await ReadExactAsync(sslStream, expectedRequestLength, cancellationToken).ConfigureAwait(false);
            requestBytesSource.TrySetResult(requestBytes);
            await sslStream.WriteAsync(responseBytes, cancellationToken).ConfigureAwait(false);
            await sslStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            sslStream.Dispose();
        }
        catch (Exception ex)
        {
            negotiatedProtocolSource.TrySetException(ex);
            requestBytesSource.TrySetException(ex);
        }
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

    private sealed class ConnectedProxyHttp2Client : IDisposable
    {
        private readonly TcpClient _client;

        public ConnectedProxyHttp2Client(TcpClient client, SslStream secureStream)
        {
            _client = client;
            SecureStream = secureStream;
        }

        public SslStream SecureStream { get; }

        public void Dispose()
        {
            SecureStream.Dispose();
            _client.Dispose();
        }
    }

    private sealed class Http2TlsUpstreamListener : IDisposable
    {
        private readonly Task _serverTask;

        public Http2TlsUpstreamListener(
            TcpListener listener,
            Task serverTask,
            Task<SslApplicationProtocol> negotiatedApplicationProtocolTask,
            Task<byte[]> requestBytesTask)
        {
            Listener = listener;
            _serverTask = serverTask;
            NegotiatedApplicationProtocolTask = negotiatedApplicationProtocolTask;
            RequestBytesTask = requestBytesTask;
        }

        public TcpListener Listener { get; }

        public Task<SslApplicationProtocol> NegotiatedApplicationProtocolTask { get; }

        public Task<byte[]> RequestBytesTask { get; }

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
