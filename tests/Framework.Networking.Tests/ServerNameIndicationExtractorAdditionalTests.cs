using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Additional edge-case tests for <see cref="ServerNameIndicationExtractor" />.
/// </summary>
public sealed class ServerNameIndicationExtractorAdditionalTests
{
    /// <summary>
    ///     Verifies that an empty buffer returns null.
    /// </summary>
    [Test]
    public async Task Extract_EmptyBuffer_ReturnsNull()
    {
        var result = ServerNameIndicationExtractor.Extract(ReadOnlySequence<byte>.Empty);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that bytes shorter than the TLS header length return null.
    /// </summary>
    [Test]
    public async Task Extract_BufferShorterThanTlsHeader_ReturnsNull()
    {
        var bytes = new byte[] { 0x16, 0x03 };

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that bytes with a non-handshake record type return null.
    /// </summary>
    [Test]
    public async Task Extract_NonHandshakeRecordType_ReturnsNull()
    {
        var bytes = new byte[] { 0x17, 0x03, 0x03, 0x00, 0x00 };

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a handshake record with non-ClientHello type returns null.
    /// </summary>
    [Test]
    public async Task Extract_NonClientHelloHandshake_ReturnsNull()
    {
        var bytes = new byte[100];
        bytes[0] = 0x16;
        bytes[1] = 0x03;
        bytes[2] = 0x03;
        bytes[3] = 0x00;
        bytes[4] = 50;
        bytes[5] = 0x02; // ServerHello, not ClientHello

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a multi-extension ClientHello where SNI is not the first extension
    ///     still returns the host name.
    /// </summary>
    [Test]
    public async Task Extract_WithMultipleExtensions_FindsServerNameAfterUnsupportedExtension()
    {
        var bytes = CreateClientHelloWithMultipleExtensions("multi.example.com");

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsEqualTo("multi.example.com");
    }

    private static byte[] CreateClientHelloWithMultipleExtensions(string hostname)
    {
        var handshakeBody = new List<byte>();
        handshakeBody.Add(0x03);
        handshakeBody.Add(0x03);

        for (var index = 0; index < 32; index++)
        {
            handshakeBody.Add((byte)index);
        }

        handshakeBody.Add(0x00);
        handshakeBody.Add(0x00);
        handshakeBody.Add(0x02);
        handshakeBody.Add(0x13);
        handshakeBody.Add(0x01);
        handshakeBody.Add(0x01);
        handshakeBody.Add(0x00);

        var unsupportedExtension = new byte[] { 0x00, 0x0A, 0x00, 0x00 };
        var sniExtension = CreateSniExtension(hostname);
        var extensionsLength = unsupportedExtension.Length + sniExtension.Length;
        handshakeBody.Add((byte)(extensionsLength >> 8));
        handshakeBody.Add((byte)extensionsLength);
        handshakeBody.AddRange(unsupportedExtension);
        handshakeBody.AddRange(sniExtension);

        var recordBytes = new List<byte>();
        recordBytes.Add(0x16);
        recordBytes.Add(0x03);
        recordBytes.Add(0x01);
        var handshakeLength = handshakeBody.Count;
        var recordLength = handshakeLength + 4;
        recordBytes.Add((byte)(recordLength >> 8));
        recordBytes.Add((byte)recordLength);
        recordBytes.Add(0x01);
        recordBytes.Add((byte)(handshakeLength >> 16));
        recordBytes.Add((byte)(handshakeLength >> 8));
        recordBytes.Add((byte)handshakeLength);
        recordBytes.AddRange(handshakeBody);
        return recordBytes.ToArray();
    }

    private static byte[] CreateSniExtension(string hostname)
    {
        var hostNameBytes = Encoding.ASCII.GetBytes(hostname);
        var extensionData = new List<byte>();
        var serverNameListLength = hostNameBytes.Length + 3;
        extensionData.Add((byte)(serverNameListLength >> 8));
        extensionData.Add((byte)serverNameListLength);
        extensionData.Add(0x00);
        extensionData.Add((byte)(hostNameBytes.Length >> 8));
        extensionData.Add((byte)hostNameBytes.Length);
        extensionData.AddRange(hostNameBytes);

        var extension = new List<byte>();
        extension.Add(0x00);
        extension.Add(0x00);
        extension.Add((byte)(extensionData.Count >> 8));
        extension.Add((byte)extensionData.Count);
        extension.AddRange(extensionData);
        return extension.ToArray();
    }
}
