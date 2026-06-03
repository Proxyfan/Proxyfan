namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Result of reading one varint element from a packed repeated payload: the decoded
///     <see cref="Value" /> and the number of bytes the varint occupied
///     (<see cref="BytesConsumed" />). A dedicated type is used because the decoder helper
///     must report both pieces of information at once and the codebase forbids multiple
///     <c>out</c> parameters on a single method.
/// </summary>
public readonly record struct PackedVarintRead
{
    /// <summary>
    ///     Gets the number of bytes consumed from the payload to decode the varint.
    /// </summary>
    public int BytesConsumed { get; }

    /// <summary>
    ///     Gets the decoded varint value.
    /// </summary>
    public ulong Value { get; }

    /// <summary>
    ///     Initializes a new <see cref="PackedVarintRead" />.
    /// </summary>
    /// <param name="value">The decoded varint value.</param>
    /// <param name="bytesConsumed">The number of bytes consumed from the payload.</param>
    public PackedVarintRead(ulong value, int bytesConsumed)
    {
        Value = value;
        BytesConsumed = bytesConsumed;
    }
}
