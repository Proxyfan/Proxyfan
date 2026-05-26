using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolRequestParser" />.
/// </summary>
public sealed class HypertextTransferProtocolRequestParserTests
{
    /// <summary>
    ///     Verifies that a valid GET request line and headers produce request data.
    /// </summary>
    [Test]
    public async Task ParseHeaders_GetRequest_ReturnsRequestData()
    {
        var headerBytes = Encoding.ASCII.GetBytes("GET http://example.com/path HTTP/1.1\r\nHost: example.com\r\n\r\n");

        var request = HypertextTransferProtocolRequestParser.ParseHeaders(headerBytes);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Method).IsEqualTo("GET");
        await Assert.That(request.RequestUri).IsEqualTo(new Uri("http://example.com/path"));
        await Assert.That(request.Version).IsEqualTo("HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that a valid POST request line and content length header are parsed.
    /// </summary>
    [Test]
    public async Task ParseHeaders_PostRequest_ReturnsRequestData()
    {
        var headerBytes = Encoding.ASCII.GetBytes("POST /submit HTTP/1.1\r\nHost: example.com\r\nContent-Length: 4\r\n\r\n");

        var request = HypertextTransferProtocolRequestParser.ParseHeaders(headerBytes);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Method).IsEqualTo("POST");
        await Assert.That(request.RequestUri.OriginalString).IsEqualTo("/submit");
        await Assert.That(HypertextTransferProtocolRequestParser.GetContentLength(request)).IsEqualTo(4L);
    }

    /// <summary>
    ///     Verifies that malformed request bytes return null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_MalformedRequest_ReturnsNull()
    {
        var headerBytes = Encoding.ASCII.GetBytes("GET\r\nHost: example.com\r\n\r\n");

        var request = HypertextTransferProtocolRequestParser.ParseHeaders(headerBytes);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies that a request line without an HTTP version returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_MissingVersion_ReturnsNull()
    {
        var headerBytes = Encoding.ASCII.GetBytes("GET /path\r\nHost: example.com\r\n\r\n");

        var request = HypertextTransferProtocolRequestParser.ParseHeaders(headerBytes);

        await Assert.That(request).IsNull();
    }
}
