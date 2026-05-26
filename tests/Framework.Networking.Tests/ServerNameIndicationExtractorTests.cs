using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ServerNameIndicationExtractor" />.
/// </summary>
public sealed class ServerNameIndicationExtractorTests
{
    /// <summary>
    ///     Verifies that the extractor returns the requested host name from a valid ClientHello.
    /// </summary>
    [Test]
    public async Task Extract_WhenClientHelloContainsServerNameIndication_ReturnsHostName()
    {
        var bytes = CreateClientHelloBytes("api.example.com");

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsEqualTo("api.example.com");
    }

    /// <summary>
    ///     Verifies that the extractor returns null when the ClientHello has no server name indication extension.
    /// </summary>
    [Test]
    public async Task Extract_WhenServerNameIndicationIsMissing_ReturnsNull()
    {
        var bytes = CreateClientHelloBytes(null);

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that the extractor returns null when the buffer does not contain a complete ClientHello.
    /// </summary>
    [Test]
    public async Task Extract_WhenBufferIsTruncated_ReturnsNull()
    {
        var fullBytes = CreateClientHelloBytes("api.example.com");
        var truncatedBytes = new byte[fullBytes.Length - 4];
        Array.Copy(fullBytes, truncatedBytes, truncatedBytes.Length);

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(truncatedBytes));

        await Assert.That(result).IsNull();
    }

    private static byte[] CreateClientHelloBytes(string? hostname)
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

        var extensions = CreateExtensions(hostname);
        handshakeBody.Add((byte)(extensions.Length >> 8));
        handshakeBody.Add((byte)extensions.Length);
        handshakeBody.AddRange(extensions);

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

    private static byte[] CreateExtensions(string? hostname)
    {
        if (hostname is null)
        {
            return CreateUnsupportedExtension();
        }

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

    private static byte[] CreateUnsupportedExtension()
    {
        return [0x00, 0x0A, 0x00, 0x00];
    }
}