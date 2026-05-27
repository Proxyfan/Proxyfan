using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Targeted tests for the deepest branches of <see cref="ServerNameIndicationExtractor" />
///     covering malformed extensions, non-SNI extension headers, and SNI entries with
///     non-hostname types.
/// </summary>
public sealed class ServerNameIndicationExtractorDeepBranchTests
{
    /// <summary>
    ///     Verifies that an extension whose declared length exceeds the remaining extensions
    ///     buffer returns null.
    /// </summary>
    [Test]
    public async Task Extract_ExtensionLengthOverflowsBuffer_ReturnsNull()
    {
        var bytes = BuildClientHello(out _, includeExtension: true, sniLengthOverride: 200);

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that an SNI list whose entry type is NOT hostname (0x00) returns null.
    /// </summary>
    [Test]
    public async Task Extract_SniEntryWithNonHostnameType_ReturnsNull()
    {
        var bytes = BuildClientHelloWithSniEntryType(0x01);

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that an SNI extension truncated at the list-length header returns null.
    /// </summary>
    [Test]
    public async Task Extract_SniExtensionTruncatedListHeader_ReturnsNull()
    {
        var hostname = "example.com";
        var bytes = BuildClientHelloWithSniBytes(new byte[] { 0x00, 0x01 });
        var _ = hostname;

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    private static byte[] BuildClientHello(out string hostname, bool includeExtension, int sniLengthOverride)
    {
        hostname = "example.com";
        var hostNameBytes = Encoding.ASCII.GetBytes(hostname);

        var sniExtensionData = new List<byte>();
        var listLength = hostNameBytes.Length + 3;
        sniExtensionData.Add((byte)(listLength >> 8));
        sniExtensionData.Add((byte)listLength);
        sniExtensionData.Add(0x00);
        sniExtensionData.Add((byte)(hostNameBytes.Length >> 8));
        sniExtensionData.Add((byte)hostNameBytes.Length);
        sniExtensionData.AddRange(hostNameBytes);

        var sniExtension = new List<byte>();
        sniExtension.Add(0x00);
        sniExtension.Add(0x00);
        sniExtension.Add((byte)(sniLengthOverride >> 8));
        sniExtension.Add((byte)sniLengthOverride);
        sniExtension.AddRange(sniExtensionData);

        var handshakeBody = BuildHandshakeBody(includeExtension ? sniExtension.ToArray() : System.Array.Empty<byte>());
        return WrapInRecord(handshakeBody);
    }

    private static byte[] BuildClientHelloWithSniBytes(byte[] sniExtensionDataBytes)
    {
        var sniExtension = new List<byte>();
        sniExtension.Add(0x00);
        sniExtension.Add(0x00);
        sniExtension.Add((byte)(sniExtensionDataBytes.Length >> 8));
        sniExtension.Add((byte)sniExtensionDataBytes.Length);
        sniExtension.AddRange(sniExtensionDataBytes);

        var handshakeBody = BuildHandshakeBody(sniExtension.ToArray());
        return WrapInRecord(handshakeBody);
    }

    private static byte[] BuildClientHelloWithSniEntryType(byte entryType)
    {
        var hostname = "example.com";
        var hostNameBytes = Encoding.ASCII.GetBytes(hostname);

        var sniExtensionData = new List<byte>();
        var listLength = hostNameBytes.Length + 3;
        sniExtensionData.Add((byte)(listLength >> 8));
        sniExtensionData.Add((byte)listLength);
        sniExtensionData.Add(entryType);
        sniExtensionData.Add((byte)(hostNameBytes.Length >> 8));
        sniExtensionData.Add((byte)hostNameBytes.Length);
        sniExtensionData.AddRange(hostNameBytes);

        var sniExtension = new List<byte>();
        sniExtension.Add(0x00);
        sniExtension.Add(0x00);
        sniExtension.Add((byte)(sniExtensionData.Count >> 8));
        sniExtension.Add((byte)sniExtensionData.Count);
        sniExtension.AddRange(sniExtensionData);

        var handshakeBody = BuildHandshakeBody(sniExtension.ToArray());
        return WrapInRecord(handshakeBody);
    }

    private static byte[] BuildHandshakeBody(byte[] extensionsBytes)
    {
        var body = new List<byte>();
        body.Add(0x03);
        body.Add(0x03);
        for (var index = 0; index < 32; index++)
        {
            body.Add((byte)index);
        }
        body.Add(0x00);
        body.Add(0x00);
        body.Add(0x02);
        body.Add(0x13);
        body.Add(0x01);
        body.Add(0x01);
        body.Add(0x00);
        body.Add((byte)(extensionsBytes.Length >> 8));
        body.Add((byte)extensionsBytes.Length);
        body.AddRange(extensionsBytes);
        return body.ToArray();
    }

    private static byte[] WrapInRecord(byte[] handshakeBody)
    {
        var record = new List<byte>();
        record.Add(0x16);
        record.Add(0x03);
        record.Add(0x01);
        var handshakeLength = handshakeBody.Length;
        var recordLength = handshakeLength + 4;
        record.Add((byte)(recordLength >> 8));
        record.Add((byte)recordLength);
        record.Add(0x01);
        record.Add((byte)(handshakeLength >> 16));
        record.Add((byte)(handshakeLength >> 8));
        record.Add((byte)handshakeLength);
        record.AddRange(handshakeBody);
        return record.ToArray();
    }
}
