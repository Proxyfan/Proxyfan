using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEventsUpstreamStreams.Resolve" />.
/// </summary>
public sealed class ServerSentEventsUpstreamStreamsTests
{
    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsUpstreamStreams.Resolve" /> returns the
    ///     unwrapped upstream stream when no bytes were prefetched.
    /// </summary>
    [Test]
    public async Task Resolve_NoPrefetchedBytes_ReturnsUpstreamStream()
    {
        using var upstreamStream = new MemoryStream();
        var request = BuildRequest(prefetched: Array.Empty<byte>(), upstreamStream: upstreamStream);

        var resolved = ServerSentEventsUpstreamStreams.Resolve(request);

        await Assert.That(resolved).IsSameReferenceAs(upstreamStream);
    }

    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsUpstreamStreams.Resolve" /> wraps the
    ///     upstream stream in a <see cref="PrefixedReadStream" /> when prefetched bytes are
    ///     present, and that the prefetched bytes are returned to the reader before the
    ///     upstream payload.
    /// </summary>
    [Test]
    public async Task Resolve_WithPrefetchedBytes_PrependsBytesBeforeUpstream()
    {
        var prefetched = new byte[] { 1, 2, 3 };
        using var upstreamStream = new MemoryStream(new byte[] { 4, 5, 6 });
        var request = BuildRequest(prefetched, upstreamStream);

        var resolved = ServerSentEventsUpstreamStreams.Resolve(request);

        await Assert.That(resolved).IsNotSameReferenceAs(upstreamStream);
        await Assert.That(resolved).IsTypeOf<PrefixedReadStream>();

        var buffer = new byte[6];
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await resolved.ReadAsync(buffer.AsMemory(totalRead));
            if (read == 0)
            {
                break;
            }
            totalRead += read;
        }

        await Assert.That(totalRead).IsEqualTo(6);
        await Assert.That(buffer).IsEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 });
    }

    private static ServerSentEventsStreamRequest BuildRequest(byte[] prefetched, Stream upstreamStream)
    {
        var requestHeaders = HeaderCollection.Empty.Add("Host", "example.com");
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = requestHeaders,
            Method = "GET",
            RequestUri = new Uri("http://example.com/events"),
            Version = "HTTP/1.1",
        });
        var responseHeaders = HeaderCollection.Empty.Add("Content-Type", "text/event-stream");
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = responseHeaders,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        return new ServerSentEventsStreamRequest
        {
            Connection = new StubProxyConnection(),
            EffectiveRequest = request,
            Flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:5678", DateTimeOffset.UtcNow),
            ResponseHeaderBytes = Array.Empty<byte>(),
            ResponseHeaders = response,
            UpstreamPrefetched = prefetched,
            UpstreamStream = upstreamStream,
        };
    }
}
