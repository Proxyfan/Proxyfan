using System;
using System.Text;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="RawHypertextTransferProtocolMessageFormatter" />.
/// </summary>
public sealed class RawHypertextTransferProtocolMessageFormatterTests
{
    /// <summary>
    ///     Verifies that a null request produces an empty string.
    /// </summary>
    [Test]
    public async Task FormatRequest_NullRequest_ReturnsEmpty()
    {
        var result = RawHypertextTransferProtocolMessageFormatter.FormatRequest(null);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a null response produces an empty string.
    /// </summary>
    [Test]
    public async Task FormatResponse_NullResponse_ReturnsEmpty()
    {
        var result = RawHypertextTransferProtocolMessageFormatter.FormatResponse(null);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that the request line, headers, blank line, and body are emitted.
    /// </summary>
    [Test]
    public async Task FormatRequest_FullRequest_IncludesAllSegments()
    {
        var headers = HeaderCollection.Empty
            .Add("Host", "example.com")
            .Add("Content-Length", "5");
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = headers,
            Method = "POST",
            RequestUri = new Uri("https://example.com/api"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(requestParameters);

        var result = RawHypertextTransferProtocolMessageFormatter.FormatRequest(request);

        await Assert.That(result.Contains("POST https://example.com/api HTTP/1.1", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Host: example.com", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Content-Length: 5", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.EndsWith("hello", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that an empty request body still emits a trailing blank line.
    /// </summary>
    [Test]
    public async Task FormatRequest_EmptyBody_EndsWithBlankLine()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(requestParameters);

        var result = RawHypertextTransferProtocolMessageFormatter.FormatRequest(request);

        await Assert.That(result.Contains("Host: example.com", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.EndsWith('\n')).IsTrue();
    }

    /// <summary>
    ///     Verifies that the response status line, headers, blank line, and body are emitted.
    /// </summary>
    [Test]
    public async Task FormatResponse_FullResponse_IncludesAllSegments()
    {
        var headers = HeaderCollection.Empty
            .Add("Content-Type", "text/plain")
            .Add("Content-Length", "5");
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);

        var result = RawHypertextTransferProtocolMessageFormatter.FormatResponse(response);

        await Assert.That(result.Contains("HTTP/1.1 200 OK", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Content-Type: text/plain", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Content-Length: 5", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.EndsWith("hello", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that a multi-value header is emitted on multiple lines.
    /// </summary>
    [Test]
    public async Task FormatResponse_MultiValueHeader_EmitsMultipleLines()
    {
        var headers = HeaderCollection.Empty
            .Add("Set-Cookie", "a=1")
            .Add("Set-Cookie", "b=2");
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);

        var result = RawHypertextTransferProtocolMessageFormatter.FormatResponse(response);

        await Assert.That(result.Contains("Set-Cookie: a=1", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Set-Cookie: b=2", StringComparison.Ordinal)).IsTrue();
    }
}
