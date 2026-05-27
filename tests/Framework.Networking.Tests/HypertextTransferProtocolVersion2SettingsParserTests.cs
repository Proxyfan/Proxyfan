using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2SettingsParser" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2SettingsParserTests
{
    /// <summary>
    ///     An empty payload is valid (zero parameters) and yields an empty list.
    /// </summary>
    [Test]
    public async Task Parse_EmptyPayload_ReturnsEmptyList()
    {
        var result = HypertextTransferProtocolVersion2SettingsParser.Parse([]);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     A 6-octet payload is parsed as a single SETTINGS_HEADER_TABLE_SIZE parameter.
    /// </summary>
    [Test]
    public async Task Parse_SingleParameter_ReturnsOneParameter()
    {
        byte[] payload = [0x00, 0x01, 0x00, 0x00, 0x10, 0x00];

        var result = HypertextTransferProtocolVersion2SettingsParser.Parse(payload);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Count).IsEqualTo(1);
        await Assert.That(result[0].Identifier).IsEqualTo((ushort)1);
        await Assert.That(result[0].IsKnownIdentifier).IsTrue();
        await Assert.That(result[0].KnownIdentifier).IsEqualTo(HypertextTransferProtocolVersion2SettingIdentifier.HeaderTableSize);
        await Assert.That(result[0].Value).IsEqualTo((uint)4096);
    }

    /// <summary>
    ///     A 12-octet payload yields two parameters (HEADER_TABLE_SIZE then ENABLE_PUSH=0).
    /// </summary>
    [Test]
    public async Task Parse_TwoParameters_ReturnsBoth()
    {
        byte[] payload =
        [
            0x00, 0x01, 0x00, 0x00, 0x10, 0x00,
            0x00, 0x02, 0x00, 0x00, 0x00, 0x00,
        ];

        var result = HypertextTransferProtocolVersion2SettingsParser.Parse(payload);

        await Assert.That(result!.Count).IsEqualTo(2);
        await Assert.That(result[1].KnownIdentifier).IsEqualTo(HypertextTransferProtocolVersion2SettingIdentifier.EnablePush);
        await Assert.That(result[1].Value).IsEqualTo((uint)0);
    }

    /// <summary>
    ///     A payload whose length is not a multiple of 6 is invalid (FRAME_SIZE_ERROR) — null is returned.
    /// </summary>
    [Test]
    public async Task Parse_MalformedLength_ReturnsNull()
    {
        byte[] payload = [0x00, 0x01, 0x00, 0x00, 0x10];

        var result = HypertextTransferProtocolVersion2SettingsParser.Parse(payload);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Unknown identifiers are reported with <c>IsKnownIdentifier</c> false so callers can ignore them.
    /// </summary>
    [Test]
    public async Task Parse_UnknownIdentifier_ReportsNotKnown()
    {
        byte[] payload = [0x00, 0xFF, 0x00, 0x00, 0x00, 0x01];

        var result = HypertextTransferProtocolVersion2SettingsParser.Parse(payload);

        await Assert.That(result!.Count).IsEqualTo(1);
        await Assert.That(result[0].Identifier).IsEqualTo((ushort)0xFF);
        await Assert.That(result[0].IsKnownIdentifier).IsFalse();
    }
}
