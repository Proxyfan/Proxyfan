using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Edge-case tests for <see cref="HypertextTransferProtocolRequestParser" /> targeting
///     the empty-header-section branch (request line followed immediately by the blank line).
/// </summary>
public sealed class HypertextTransferProtocolRequestParserEdgeCaseTests
{
    /// <summary>
    ///     Verifies that a request with no header lines (request line + blank line) parses
    ///     successfully with an empty header collection.
    /// </summary>
    [Test]
    public async Task Parse_RequestWithNoHeaders_ReturnsRequestWithEmptyHeaders()
    {
        var bytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");

        var parsed = HypertextTransferProtocolRequestParser.ParseHeaders(bytes);

        await Assert.That(parsed).IsNotNull();
        await Assert.That(parsed!.Method).IsEqualTo("GET");
        await Assert.That(parsed.Headers.Count).IsEqualTo(0);
    }
}
