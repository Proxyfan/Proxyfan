namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bit-layout parameters that describe how to encode an HPACK literal representation —
///     the leading flag byte (e.g. 0x40 for incremental indexing, 0x10 for never-indexed)
///     and the prefix-bit width of its integer index field.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackLiteralLayout
{
    /// <summary>
    ///     Gets the leading flag byte pattern applied to the high bits of the first
    ///     emitted byte (0x40, 0x10, or 0x00 in HPACK).
    /// </summary>
    public required byte FlagByte { get; init; }

    /// <summary>
    ///     Gets the prefix-bit width used for the index integer (6 for incremental
    ///     indexing; 4 for never-indexed and without-indexing).
    /// </summary>
    public required int PrefixBits { get; init; }
}
