using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2SettingsWriter" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2SettingsWriterTests
{
    /// <summary>
    ///     Each parameter takes six octets — the empty list takes zero.
    /// </summary>
    [Test]
    public async Task ComputeByteSize_KnownParameterCount_ReturnsSixTimesCount()
    {
        var parameters = new List<HypertextTransferProtocolVersion2SettingParameter>
        {
            new(1, 0x1000),
            new(3, 0x100),
        };

        var byteSize = HypertextTransferProtocolVersion2SettingsWriter.ComputeByteSize(parameters);

        await Assert.That(byteSize).IsEqualTo(12);
    }

    /// <summary>
    ///     The encoded payload round-trips through the parser.
    /// </summary>
    [Test]
    public async Task Write_TwoParameters_RoundTripsThroughParser()
    {
        var parameters = new List<HypertextTransferProtocolVersion2SettingParameter>
        {
            new(1, 0x4000),
            new(4, 0xFFFF),
        };
        var buffer = new byte[parameters.Count * 6];

        var written = HypertextTransferProtocolVersion2SettingsWriter.Write(buffer, parameters);
        var parsed = HypertextTransferProtocolVersion2SettingsParser.Parse(buffer);

        await Assert.That(written).IsEqualTo(buffer.Length);
        await Assert.That(parsed).IsNotNull();
        await Assert.That(parsed!.Count).IsEqualTo(2);
        await Assert.That(parsed[0].Identifier).IsEqualTo((ushort)1);
        await Assert.That(parsed[0].Value).IsEqualTo((uint)0x4000);
        await Assert.That(parsed[1].Identifier).IsEqualTo((ushort)4);
        await Assert.That(parsed[1].Value).IsEqualTo((uint)0xFFFF);
    }

    /// <summary>
    ///     A destination buffer too small for the payload is rejected.
    /// </summary>
    [Test]
    public async Task Write_DestinationTooSmall_Throws()
    {
        var parameters = new List<HypertextTransferProtocolVersion2SettingParameter> { new(1, 0) };
        var buffer = new byte[5];

        await Assert.That(() => HypertextTransferProtocolVersion2SettingsWriter.Write(buffer, parameters))
            .Throws<ArgumentException>();
    }
}
