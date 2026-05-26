using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Additional tests for <see cref="HypertextTransferProtocolRequestParser" /> and
///     <see cref="HypertextTransferProtocolResponseParser" /> covering edge cases.
/// </summary>
public sealed class HypertextTransferProtocolRequestParserAdditionalTests
{
    /// <summary>
    ///     Verifies that a request without the terminating blank line returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_NoTerminator_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: example.com\r\n");

        var result = HypertextTransferProtocolRequestParser.ParseHeaders(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a request line with too few parts returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_TooFewParts_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes("GET\r\n\r\n");

        var result = HypertextTransferProtocolRequestParser.ParseHeaders(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a request line without an HTTP version returns null.
    /// </summary>
    [Test]
    public async Task ParseHeaders_InvalidVersion_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes("GET / NOTHTTP/1.1\r\n\r\n");

        var result = HypertextTransferProtocolRequestParser.ParseHeaders(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRequestParser.GetContentLength" /> returns
    ///     zero for a request without a Content-Length header.
    /// </summary>
    [Test]
    public async Task GetContentLength_NoHeader_ReturnsZero()
    {
        var bytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = HypertextTransferProtocolRequestParser.ParseHeaders(bytes);

        var length = HypertextTransferProtocolRequestParser.GetContentLength(request!);

        await Assert.That(length).IsEqualTo(0L);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRequestParser.GetContentLength" /> returns
    ///     zero when the Content-Length header is a non-numeric value.
    /// </summary>
    [Test]
    public async Task GetContentLength_NonNumericHeader_ReturnsZero()
    {
        var bytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nContent-Length: abc\r\n\r\n");
        var request = HypertextTransferProtocolRequestParser.ParseHeaders(bytes);

        var length = HypertextTransferProtocolRequestParser.GetContentLength(request!);

        await Assert.That(length).IsEqualTo(0L);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRequestParser.GetContentLength" /> returns
    ///     the parsed value when the Content-Length header is a positive integer.
    /// </summary>
    [Test]
    public async Task GetContentLength_PositiveHeader_ReturnsValue()
    {
        var bytes = Encoding.ASCII.GetBytes("POST / HTTP/1.1\r\nContent-Length: 42\r\n\r\n");
        var request = HypertextTransferProtocolRequestParser.ParseHeaders(bytes);

        var length = HypertextTransferProtocolRequestParser.GetContentLength(request!);

        await Assert.That(length).IsEqualTo(42L);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRequestParser.GetContentLength" /> returns
    ///     zero for a negative value (treated as invalid).
    /// </summary>
    [Test]
    public async Task GetContentLength_NegativeHeader_ReturnsZero()
    {
        var bytes = Encoding.ASCII.GetBytes("POST / HTTP/1.1\r\nContent-Length: -5\r\n\r\n");
        var request = HypertextTransferProtocolRequestParser.ParseHeaders(bytes);

        var length = HypertextTransferProtocolRequestParser.GetContentLength(request!);

        await Assert.That(length).IsEqualTo(0L);
    }
}
