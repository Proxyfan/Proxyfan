namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Result of decoding a single varint from a packed-repeated payload: the decoded
///     value plus the byte offset positioned immediately after the consumed bytes.
/// </summary>
public readonly record struct ProtobufPackedVarintRead
{
    /// <summary>
    ///     Gets the byte offset immediately after the decoded varint.
    /// </summary>
    public int NextOffset { get; init; }

    /// <summary>
    ///     Gets the decoded varint value.
    /// </summary>
    public ulong Value { get; init; }
}
