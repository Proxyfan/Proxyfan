using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2GoAwayParser" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2GoAwayParserTests
{
    /// <summary>
    ///     A minimum 8-octet payload (no debug data) is parsed.
    /// </summary>
    [Test]
    public async Task Parse_MinimalPayload_ExposesIdentifiersAndCode()
    {
        byte[] payload =
        [
            0x80, 0x00, 0x00, 0x09,
            0x00, 0x00, 0x00, 0x07,
        ];

        var result = HypertextTransferProtocolVersion2GoAwayParser.Parse(payload);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.LastStreamIdentifier).IsEqualTo((uint)9);
        await Assert.That(result.Value.ErrorCode).IsEqualTo((uint)7);
        await Assert.That(result.Value.AdditionalDebugData.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Trailing octets become the AdditionalDebugData buffer.
    /// </summary>
    [Test]
    public async Task Parse_WithDebugData_ExposesDebugBytes()
    {
        byte[] payload =
        [
            0x00, 0x00, 0x00, 0x05,
            0x00, 0x00, 0x00, 0x01,
            0x68, 0x69,
        ];

        var result = HypertextTransferProtocolVersion2GoAwayParser.Parse(payload);

        await Assert.That(result!.Value.AdditionalDebugData.ToArray()).IsEquivalentTo(new byte[] { 0x68, 0x69 });
    }

    /// <summary>
    ///     A payload shorter than the 8-octet prefix is malformed.
    /// </summary>
    [Test]
    public async Task Parse_TooShortPayload_ReturnsNull()
    {
        byte[] payload = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        var result = HypertextTransferProtocolVersion2GoAwayParser.Parse(payload);

        await Assert.That(result).IsNull();
    }
}
