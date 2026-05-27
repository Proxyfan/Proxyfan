using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2ResetStreamParser" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2ResetStreamParserTests
{
    /// <summary>
    ///     A four-octet payload yields the error code.
    /// </summary>
    [Test]
    public async Task Parse_FourOctets_ReturnsErrorCode()
    {
        byte[] payload = [0x00, 0x00, 0x00, 0x05];

        var result = HypertextTransferProtocolVersion2ResetStreamParser.Parse(payload);

        await Assert.That(result).IsEqualTo((uint)5);
    }

    /// <summary>
    ///     A payload longer than 4 octets is malformed.
    /// </summary>
    [Test]
    public async Task Parse_TooLong_ReturnsNull()
    {
        byte[] payload = [0x00, 0x00, 0x00, 0x00, 0x00];

        var result = HypertextTransferProtocolVersion2ResetStreamParser.Parse(payload);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     A payload shorter than 4 octets is malformed.
    /// </summary>
    [Test]
    public async Task Parse_TooShort_ReturnsNull()
    {
        byte[] payload = [0x00, 0x00, 0x00];

        var result = HypertextTransferProtocolVersion2ResetStreamParser.Parse(payload);

        await Assert.That(result).IsNull();
    }
}
