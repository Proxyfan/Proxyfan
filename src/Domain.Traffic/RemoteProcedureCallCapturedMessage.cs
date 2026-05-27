using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     A single captured Remote Procedure Call (gRPC) message within a streaming flow.
///     Records the message direction, the wire compression flag, the (possibly compressed)
///     payload bytes and the capture timestamp.
/// </summary>
public sealed class RemoteProcedureCallCapturedMessage
{
    /// <summary>
    ///     Gets the direction of the captured message.
    /// </summary>
    public RemoteProcedureCallDirection Direction { get; }

    /// <summary>
    ///     Gets a value indicating whether the payload was sent with the gRPC compression
    ///     flag set. Proxyfan never decompresses; consumers handle that themselves.
    /// </summary>
    public bool IsCompressed { get; }

    /// <summary>
    ///     Gets the (possibly compressed) protobuf payload bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    ///     Gets the wall-clock timestamp at which the message was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallCapturedMessage" />.
    /// </summary>
    /// <param name="direction">The message direction.</param>
    /// <param name="isCompressed">Whether the payload was compressed.</param>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="timestamp">The capture timestamp.</param>
    public RemoteProcedureCallCapturedMessage(
        RemoteProcedureCallDirection direction,
        bool isCompressed,
        ReadOnlyMemory<byte> payload,
        DateTimeOffset timestamp)
    {
        Direction = direction;
        IsCompressed = isCompressed;
        Payload = payload;
        Timestamp = timestamp;
    }
}
