using Proxyfan.Domain.Traffic;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ComposerRequestMessageBuilder" />.
/// </summary>
public sealed class ComposerRequestMessageBuilderTests
{
    /// <summary>
    ///     Verifies that the builder sets the method, URI, and version from the request data.
    /// </summary>
    [Test]
    public async Task Build_DeleteRequest_SetsMethodAndUri()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "DELETE",
            RequestUri = new Uri("https://example.com/items/42"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        using var message = ComposerRequestMessageBuilder.Build(request);

        await Assert.That(message.Method.Method).IsEqualTo("DELETE");
        await Assert.That(message.RequestUri!.ToString()).IsEqualTo("https://example.com/items/42");
    }

    /// <summary>
    ///     Verifies that a body produces a content payload of the correct length.
    /// </summary>
    [Test]
    public async Task Build_WithBody_PopulatesContent()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = HeaderCollection.Empty,
            Method = "POST",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        using var message = ComposerRequestMessageBuilder.Build(request);

        await Assert.That(message.Content).IsNotNull();
        var actual = await message.Content!.ReadAsStringAsync(System.Threading.CancellationToken.None);
        await Assert.That(actual).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that a Content-Type header is added to the content rather than the message
    ///     headers (since <see cref="HttpRequestHeaders" /> rejects content headers).
    /// </summary>
    [Test]
    public async Task Build_ContentTypeHeader_GoesToContent()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("{}"),
            Headers = HeaderCollection.Empty.Add("Content-Type", "application/json"),
            Method = "POST",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        using var message = ComposerRequestMessageBuilder.Build(request);

        await Assert.That(message.Content!.Headers.ContentType!.MediaType).IsEqualTo("application/json");
    }

    /// <summary>
    ///     Verifies that ordinary request headers are added to the message headers collection.
    /// </summary>
    [Test]
    public async Task Build_CustomHeader_GoesToMessageHeaders()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("X-Custom", "value"),
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        using var message = ComposerRequestMessageBuilder.Build(request);

        await Assert.That(message.Headers.Contains("X-Custom")).IsTrue();
    }

    /// <summary>
    ///     Verifies proxy-specific and hop-by-hop headers are stripped before replaying a request
    ///     directly to the origin server.
    /// </summary>
    [Test]
    public async Task Build_ProxyAndConnectionHeaders_StripsBeforeReplay()
    {
        var headers = HeaderCollection.Empty
            .Add("Proxy-Authorization", "Basic cHJveHk6c2VjcmV0")
            .Add("Proxy-Connection", "keep-alive")
            .Add("Connection", "X-Remove, keep-alive")
            .Add("Keep-Alive", "timeout=10")
            .Add("TE", "trailers")
            .Add("Trailer", "Expires")
            .Add("Transfer-Encoding", "chunked")
            .Add("Upgrade", "websocket")
            .Add("X-Remove", "leak")
            .Add("X-Preserved", "value");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        using var message = ComposerRequestMessageBuilder.Build(request);

        await Assert.That(message.Headers.Contains("Proxy-Authorization")).IsFalse();
        await Assert.That(message.Headers.Contains("Proxy-Connection")).IsFalse();
        await Assert.That(message.Headers.Contains("Connection")).IsFalse();
        await Assert.That(message.Headers.Contains("Keep-Alive")).IsFalse();
        await Assert.That(message.Headers.Contains("TE")).IsFalse();
        await Assert.That(message.Headers.Contains("Trailer")).IsFalse();
        await Assert.That(message.Headers.Contains("Transfer-Encoding")).IsFalse();
        await Assert.That(message.Headers.Contains("Upgrade")).IsFalse();
        await Assert.That(message.Headers.Contains("X-Remove")).IsFalse();
        await Assert.That(message.Headers.Contains("X-Preserved")).IsTrue();
    }
}
