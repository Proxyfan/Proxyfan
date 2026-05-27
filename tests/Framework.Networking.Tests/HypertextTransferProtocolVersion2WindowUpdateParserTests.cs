using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2WindowUpdateParser" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2WindowUpdateParserTests
{
    /// <summary>
    ///     A 4-octet positive increment is returned as-is (top reserved bit ignored).
    /// </summary>
    [Test]
    public async Task Parse_PositiveIncrement_ReturnsValue()
    {
        byte[] payload = [0x00, 0x00, 0x10, 0x00];

        var result = HypertextTransferProtocolVersion2WindowUpdateParser.Parse(payload);

        await Assert.That(result).IsEqualTo(4096);
    }

    /// <summary>
    ///     The top reserved bit (R) is masked off before interpreting the increment value.
    /// </summary>
    [Test]
    public async Task Parse_TopBitSet_IsIgnored()
    {
        byte[] payload = [0x80, 0x00, 0x10, 0x00];

        var result = HypertextTransferProtocolVersion2WindowUpdateParser.Parse(payload);

        await Assert.That(result).IsEqualTo(4096);
    }

    /// <summary>
    ///     A zero increment is illegal (PROTOCOL_ERROR/FLOW_CONTROL_ERROR per RFC 7540 § 6.9)
    ///     and the parser returns <c>null</c>.
    /// </summary>
    [Test]
    public async Task Parse_ZeroIncrement_ReturnsNull()
    {
        byte[] payload = [0x00, 0x00, 0x00, 0x00];

        var result = HypertextTransferProtocolVersion2WindowUpdateParser.Parse(payload);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     A payload that is not exactly 4 octets is a FRAME_SIZE_ERROR — null is returned.
    /// </summary>
    [Test]
    public async Task Parse_WrongLength_ReturnsNull()
    {
        byte[] payload = [0x00, 0x00, 0x10];

        var result = HypertextTransferProtocolVersion2WindowUpdateParser.Parse(payload);

        await Assert.That(result).IsNull();
    }
}
