using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEventsRelayRequest" />.
/// </summary>
public sealed class ServerSentEventsRelayRequestTests
{
    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsRelayRequest" /> exposes all configured
    ///     init properties verbatim.
    /// </summary>
    [Test]
    public async Task InitProperties_AllAssigned_RoundTripValues()
    {
        var forwarding = new HypertextTransferProtocolForwardingRequest
        {
            Connection = new StubProxyConnection(),
            EffectiveRequest = BuildRequestData(),
            Flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow),
            RequestExchange = new HypertextTransferProtocolProxyRequestExchange(ReadOnlyMemory<byte>.Empty, Array.Empty<byte>(), BuildRequestData()),
        };
        var headerRead = new HypertextTransferProtocolResponseHeaderRead { Response = BuildResponseData(), HeaderBytes = Array.Empty<byte>() };
        var pipe = new System.IO.Pipelines.Pipe();
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        var serverTask = Task.Run(async () =>
        {
            using var accepted = await listener.AcceptTcpClientAsync();
            accepted.GetStream();
        });
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(System.Net.IPAddress.Loopback, port);
        await serverTask;
        var network = client.GetStream();

        try
        {
            var relayRequest = new ServerSentEventsRelayRequest
            {
                ForwardingRequest = forwarding,
                HeaderRead = headerRead,
                Reader = pipe.Reader,
                UpstreamStream = network,
            };

            await Assert.That(relayRequest.ForwardingRequest).IsSameReferenceAs(forwarding);
            await Assert.That(relayRequest.HeaderRead).IsSameReferenceAs(headerRead);
            await Assert.That(relayRequest.Reader).IsSameReferenceAs(pipe.Reader);
            await Assert.That(relayRequest.UpstreamStream).IsSameReferenceAs(network);
        }
        finally
        {
            await network.DisposeAsync();
            listener.Stop();
        }
    }

    private static HypertextTransferProtocolRequestData BuildRequestData()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        return new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("http://example.com/"),
            Version = "HTTP/1.1",
        });
    }

    private static HypertextTransferProtocolResponseData BuildResponseData()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "0");
        return new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
    }
}
