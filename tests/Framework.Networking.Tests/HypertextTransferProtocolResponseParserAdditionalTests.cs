using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Additional tests for <see cref="HypertextTransferProtocolResponseParser" />.
/// </summary>
public sealed class HypertextTransferProtocolResponseParserAdditionalTests
{
    /// <summary>
    ///     Verifies that a response without the terminating blank line returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_NoTerminator_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\n");

        var result = HypertextTransferProtocolResponseParser.ParseHeaders(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a response with no status line returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_EmptyStatusLine_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes("\r\n\r\n");

        var result = HypertextTransferProtocolResponseParser.ParseHeaders(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a response with a non-numeric status code returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_NonNumericStatusCode_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes("HTTP/1.1 ABC OK\r\n\r\n");

        var result = HypertextTransferProtocolResponseParser.ParseHeaders(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a response with a missing reason phrase returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_MissingReasonPhrase_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes("HTTP/1.1 200\r\n\r\n");

        var result = HypertextTransferProtocolResponseParser.ParseHeaders(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a response with an invalid version prefix returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_InvalidVersionPrefix_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes("FTP/1.0 200 OK\r\n\r\n");

        var result = HypertextTransferProtocolResponseParser.ParseHeaders(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolResponseParser.GetContentLength" />
    ///     returns -1 (unknown) when the Content-Length header is missing.
    /// </summary>
    [Test]
    public async Task GetContentLength_MissingHeader_ReturnsNegativeOne()
    {
        var bytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n\r\n");
        var response = HypertextTransferProtocolResponseParser.ParseHeaders(bytes);

        var length = HypertextTransferProtocolResponseParser.GetContentLength(response!);

        await Assert.That(length).IsEqualTo(-1L);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolResponseParser.GetContentLength" />
    ///     returns the parsed value when the Content-Length header is a positive integer.
    /// </summary>
    [Test]
    public async Task GetContentLength_PositiveHeader_ReturnsValue()
    {
        var bytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 17\r\n\r\n");
        var response = HypertextTransferProtocolResponseParser.ParseHeaders(bytes);

        var length = HypertextTransferProtocolResponseParser.GetContentLength(response!);

        await Assert.That(length).IsEqualTo(17L);
    }
}
