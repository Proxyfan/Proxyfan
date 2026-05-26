using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Extracts the requested server name indication host name from a transport layer security ClientHello record.
/// </summary>
public static class ServerNameIndicationExtractor
{
    private const byte ClientHelloHandshakeType = 0x01;
    private const byte HandshakeRecordType = 0x16;
    private const byte HostNameEntryType = 0x00;
    private const ushort ServerNameIndicationExtensionType = 0x0000;
    private const int TransportLayerSecurityHeaderLength = 5;

    /// <summary>
    ///     Attempts to extract the requested server name indication host name from the provided bytes.
    /// </summary>
    /// <param name="bytes">The bytes containing the beginning of a transport layer security handshake.</param>
    /// <returns>The requested host name when present; otherwise, <see langword="null" />.</returns>
    public static string? Extract(ReadOnlySequence<byte> bytes)
    {
        var buffer = bytes.ToArray();

        if (!HasValidTransportLayerSecurityRecord(buffer))
        {
            return null;
        }

        if (!HasExtensionsRange(buffer, out Range extensionsRange))
        {
            return null;
        }

        return TryReadServerNameIndication(buffer, extensionsRange);
    }

    private static bool HasExtensionsRange(byte[] buffer, out Range extensionsRange)
    {
        extensionsRange = default;
        var offset = 43;

        if (!HasVariableLengthBlock(buffer, offset, 1, out offset))
        {
            return false;
        }

        if (!HasVariableLengthBlock(buffer, offset, 2, out offset))
        {
            return false;
        }

        if (!HasVariableLengthBlock(buffer, offset, 1, out offset))
        {
            return false;
        }

        if (offset + 2 > buffer.Length)
        {
            return false;
        }

        var start = offset + 2;
        var end = start + ReadUnsigned16BitInteger(buffer, offset);

        if (end > buffer.Length)
        {
            return false;
        }

        var range = new Range(start, end);
        extensionsRange = range;
        return true;
    }

    private static bool HasValidTransportLayerSecurityRecord(byte[] buffer)
    {
        if (buffer.Length < TransportLayerSecurityHeaderLength || buffer[0] != HandshakeRecordType)
        {
            return false;
        }

        var recordLength = ReadUnsigned16BitInteger(buffer, 3);

        if (buffer.Length < TransportLayerSecurityHeaderLength + recordLength || buffer.Length < 43 || buffer[5] != ClientHelloHandshakeType)
        {
            return false;
        }

        var handshakeLength = ReadUnsigned24BitInteger(buffer, 6);
        return buffer.Length >= 9 + handshakeLength;
    }

    private static bool HasVariableLengthBlock(byte[] buffer, int offset, int lengthFieldSize, out int nextOffset)
    {
        nextOffset = offset;

        if (offset + lengthFieldSize > buffer.Length)
        {
            return false;
        }

        var blockLength = lengthFieldSize == 1
            ? buffer[offset]
            : ReadUnsigned16BitInteger(buffer, offset);
        var blockOffset = offset + lengthFieldSize;
        nextOffset = blockOffset + blockLength;
        return nextOffset <= buffer.Length;
    }

    private static ushort ReadUnsigned16BitInteger(byte[] buffer, int offset)
    {
        var bytes = new ReadOnlySpan<byte>(buffer, offset, 2);
        return BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    private static int ReadUnsigned24BitInteger(byte[] buffer, int offset)
    {
        return (buffer[offset] << 16) | (buffer[offset + 1] << 8) | buffer[offset + 2];
    }

    private static string? TryReadServerNameIndication(byte[] buffer, Range extensionsRange)
    {
        var offset = extensionsRange.Start.Value;
        var extensionsEnd = extensionsRange.End.Value;

        while (offset + 4 <= extensionsEnd)
        {
            var extensionType = ReadUnsigned16BitInteger(buffer, offset);
            var extensionLength = ReadUnsigned16BitInteger(buffer, offset + 2);
            var extensionDataOffset = offset + 4;
            var extensionDataEnd = extensionDataOffset + extensionLength;

            if (extensionDataEnd > extensionsEnd)
            {
                return null;
            }

            if (extensionType == ServerNameIndicationExtensionType)
            {
                return TryReadServerNameIndicationHostName(buffer, extensionDataOffset, extensionDataEnd);
            }

            offset = extensionDataEnd;
        }

        return null;
    }

    private static string? TryReadServerNameIndicationHostName(byte[] buffer, int offset, int extensionDataEnd)
    {
        if (offset + 5 > extensionDataEnd)
        {
            return null;
        }

        var listLength = ReadUnsigned16BitInteger(buffer, offset);
        var listOffset = offset + 2;
        var listEnd = listOffset + listLength;

        if (listEnd > extensionDataEnd || buffer[listOffset] != HostNameEntryType)
        {
            return null;
        }

        var nameLength = ReadUnsigned16BitInteger(buffer, listOffset + 1);
        var nameOffset = listOffset + 3;

        if (nameOffset + nameLength > listEnd)
        {
            return null;
        }

        return Encoding.ASCII.GetString(buffer, nameOffset, nameLength);
    }
}