using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Additional edge-case tests for <see cref="HypertextTransferProtocolHeaderParser" />.
/// </summary>
public sealed class HypertextTransferProtocolHeaderParserAdditionalTests
{
    /// <summary>
    ///     Verifies that a header line starting with a colon (empty name) is skipped.
    /// </summary>
    [Test]
    public async Task Parse_HeaderLineStartingWithColon_IsSkipped()
    {
        const string headerSection = ":value\r\nHost: example.com";

        var headers = HypertextTransferProtocolHeaderParser.Parse(headerSection);

        await Assert.That(headers.Count).IsEqualTo(1);
        await Assert.That(headers.Get("Host")).IsEqualTo("example.com");
    }

    /// <summary>
    ///     Verifies that an empty line between two valid headers is skipped.
    /// </summary>
    [Test]
    public async Task Parse_EmptyLineBetweenHeaders_IsSkipped()
    {
        const string headerSection = "Host: example.com\r\n\r\nAccept: text/plain";

        var headers = HypertextTransferProtocolHeaderParser.Parse(headerSection);

        await Assert.That(headers.Count).IsEqualTo(2);
        await Assert.That(headers.Get("Accept")).IsEqualTo("text/plain");
    }

    /// <summary>
    ///     Verifies that a whitespace-only header name is rejected.
    /// </summary>
    [Test]
    public async Task Parse_WhitespaceOnlyName_IsSkipped()
    {
        const string headerSection = "   : value\r\nHost: example.com";

        var headers = HypertextTransferProtocolHeaderParser.Parse(headerSection);

        await Assert.That(headers.Count).IsEqualTo(1);
        await Assert.That(headers.Get("Host")).IsEqualTo("example.com");
    }

    /// <summary>
    ///     Verifies that headers with invalid token characters in the name are skipped.
    /// </summary>
    [Test]
    public async Task Parse_NameContainingSpace_IsSkipped()
    {
        const string headerSection = "Bad Name: value\r\nHost: example.com";

        var headers = HypertextTransferProtocolHeaderParser.Parse(headerSection);

        await Assert.That(headers.Count).IsEqualTo(1);
        await Assert.That(headers.Get("Host")).IsEqualTo("example.com");
    }
}
