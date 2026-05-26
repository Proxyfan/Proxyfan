using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolHeaderParser" />.
/// </summary>
public sealed class HypertextTransferProtocolHeaderParserTests
{
    /// <summary>
    ///     Verifies that valid header lines are parsed into the expected collection.
    /// </summary>
    [Test]
    public async Task Parse_ValidHeaders_ReturnsCollection()
    {
        const string headerSection = "Host: example.com\r\nContent-Length: 4";

        var headers = HypertextTransferProtocolHeaderParser.Parse(headerSection);

        await Assert.That(headers.Count).IsEqualTo(2);
        await Assert.That(headers.Get("Host")).IsEqualTo("example.com");
        await Assert.That(headers.Get("Content-Length")).IsEqualTo("4");
    }

    /// <summary>
    ///     Verifies that empty input returns the shared empty collection.
    /// </summary>
    [Test]
    public async Task Parse_EmptyInput_ReturnsEmptyCollection()
    {
        var headers = HypertextTransferProtocolHeaderParser.Parse(string.Empty);

        await Assert.That(headers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that malformed header lines without a colon are skipped.
    /// </summary>
    [Test]
    public async Task Parse_MalformedHeader_ReturnsSkippedHeader()
    {
        const string headerSection = "Host: example.com\r\nInvalidHeader\r\nAccept: text/plain";

        var headers = HypertextTransferProtocolHeaderParser.Parse(headerSection);

        await Assert.That(headers.Count).IsEqualTo(2);
        await Assert.That(headers.HasHeader("InvalidHeader")).IsFalse();
    }

    /// <summary>
    ///     Verifies that duplicate header names append additional values.
    /// </summary>
    [Test]
    public async Task Parse_DuplicateNames_ReturnsMultipleValues()
    {
        const string headerSection = "Accept: text/plain\r\nAccept: application/json";

        var headers = HypertextTransferProtocolHeaderParser.Parse(headerSection);

        await Assert.That(headers.GetAll("Accept").Length).IsEqualTo(2);
        await Assert.That(headers.GetAll("Accept")[1]).IsEqualTo("application/json");
    }
}
