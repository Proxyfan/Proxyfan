namespace Proxyfan.Framework.Networking;

/// <summary>
///     Result returned by <see cref="HypertextTransferProtocolVersion2HpackStringDecoder.Decode" /> —
///     the decoded UTF-8 string plus the number of input bytes consumed (length prefix + payload).
/// </summary>
public readonly record struct HypertextTransferProtocolVersion2HpackStringDecodeResult
{
    /// <summary>
    ///     Gets the number of bytes consumed from the source (length prefix + payload).
    /// </summary>
    public int BytesConsumed { get; }

    /// <summary>
    ///     Gets the decoded string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     Initializes a new HPACK string decode result.
    /// </summary>
    /// <param name="value">The decoded string.</param>
    /// <param name="bytesConsumed">The number of bytes consumed from the source.</param>
    public HypertextTransferProtocolVersion2HpackStringDecodeResult(string value, int bytesConsumed)
    {
        Value = value;
        BytesConsumed = bytesConsumed;
    }
}
