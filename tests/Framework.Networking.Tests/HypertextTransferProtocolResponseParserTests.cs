using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolResponseParser" />.
/// </summary>
public sealed class HypertextTransferProtocolResponseParserTests
{
    /// <summary>
    ///     Verifies that a successful response status line and headers are parsed.
    /// </summary>
    [Test]
    public async Task ParseHeaders_SuccessStatus_ReturnsResponseData()
    {
        var headerBytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\n");

        var response = HypertextTransferProtocolResponseParser.ParseHeaders(headerBytes);

        await Assert.That(response).IsNotNull();
        await Assert.That(response!.StatusCode).IsEqualTo(200);
        await Assert.That(response.ReasonPhrase).IsEqualTo("OK");
        await Assert.That(HypertextTransferProtocolResponseParser.GetContentLength(response)).IsEqualTo(2L);
    }

    /// <summary>
    ///     Verifies that a not-found response with a multi-word reason phrase is parsed.
    /// </summary>
    [Test]
    public async Task ParseHeaders_NotFoundStatus_ReturnsResponseData()
    {
        var headerBytes = Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\nServer: proxyfan\r\n\r\n");

        var response = HypertextTransferProtocolResponseParser.ParseHeaders(headerBytes);

        await Assert.That(response).IsNotNull();
        await Assert.That(response!.StatusCode).IsEqualTo(404);
        await Assert.That(response.ReasonPhrase).IsEqualTo("Not Found");
        await Assert.That(HypertextTransferProtocolResponseParser.GetContentLength(response)).IsEqualTo(-1L);
    }

    /// <summary>
    ///     Verifies that malformed response bytes return null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_MalformedStatus_ReturnsNull()
    {
        var headerBytes = Encoding.ASCII.GetBytes("BROKEN RESPONSE\r\nServer: proxyfan\r\n\r\n");

        var response = HypertextTransferProtocolResponseParser.ParseHeaders(headerBytes);

        await Assert.That(response).IsNull();
    }

    /// <summary>
    ///     Verifies that a response status line without a reason phrase returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_MissingReason_ReturnsNull()
    {
        var headerBytes = Encoding.ASCII.GetBytes("HTTP/1.1 404\r\nServer: proxyfan\r\n\r\n");

        var response = HypertextTransferProtocolResponseParser.ParseHeaders(headerBytes);

        await Assert.That(response).IsNull();
    }
}
