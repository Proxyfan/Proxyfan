using System;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     A single message extracted from a gRPC (Google Remote Procedure Call) stream. Each
///     gRPC frame begins with a 5-byte prefix: 1 byte compression flag + 4 bytes big-endian
///     length. The payload bytes follow and contain a single protobuf message.
/// </summary>
public sealed class RemoteProcedureCallMessage
{
    /// <summary>
    ///     Gets a value indicating whether the message payload was compressed (compression
    ///     flag byte non-zero). Proxyfan does not currently decompress; consumers must do so.
    /// </summary>
    public bool IsCompressed { get; }

    /// <summary>
    ///     Gets the (possibly still-compressed) message payload bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallMessage" />.
    /// </summary>
    /// <param name="isCompressed">Whether the payload is compressed.</param>
    /// <param name="payload">The (possibly still-compressed) payload bytes.</param>
    public RemoteProcedureCallMessage(bool isCompressed, ReadOnlyMemory<byte> payload)
    {
        IsCompressed = isCompressed;
        Payload = payload;
    }
}
