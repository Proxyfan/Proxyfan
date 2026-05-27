using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     The 24-octet client connection preface magic string defined by RFC 7540 § 3.5.
///     Every HTTP/2 client connection begins with this sequence followed by a SETTINGS frame.
/// </summary>
public static class HypertextTransferProtocolVersion2ConnectionPreface
{
    private const string MagicString = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
    private static readonly byte[] Bytes;

    /// <summary>
    ///     Gets the length of the preface in octets (always 24).
    /// </summary>
    public static int Length => Bytes.Length;

    static HypertextTransferProtocolVersion2ConnectionPreface()
    {
        Bytes = System.Text.Encoding.ASCII.GetBytes(MagicString);
    }

    /// <summary>
    ///     Checks whether the supplied buffer begins with the HTTP/2 client connection preface.
    /// </summary>
    /// <param name="buffer">The buffer to inspect.</param>
    /// <returns><see langword="true" /> when the buffer starts with the 24-byte preface.</returns>
    public static bool HasPreface(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < Bytes.Length)
        {
            return false;
        }

        return buffer[..Bytes.Length].SequenceEqual(Bytes);
    }

    /// <summary>
    ///     Returns the canonical preface as a freshly-allocated byte array.
    /// </summary>
    /// <returns>A copy of the preface bytes.</returns>
    public static byte[] ToArray()
    {
        var copy = new byte[Bytes.Length];
        Bytes.AsSpan().CopyTo(copy);
        return copy;
    }
}
