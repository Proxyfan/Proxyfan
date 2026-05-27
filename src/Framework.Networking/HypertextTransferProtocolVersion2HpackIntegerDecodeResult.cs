namespace Proxyfan.Framework.Networking;

/// <summary>
///     The return value of a single HPACK integer decode operation; includes the decoded
///     value and the number of source bytes consumed. Returned as nullable from the
///     codec methods — a null value indicates an underflow or overflow.
/// </summary>
public readonly record struct HypertextTransferProtocolVersion2HpackIntegerDecodeResult
{
    /// <summary>
    ///     Gets the number of source bytes consumed during decoding.
    /// </summary>
    public int BytesConsumed { get; }

    /// <summary>
    ///     Gets the decoded integer value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    ///     Initializes a new HPACK integer decode result.
    /// </summary>
    /// <param name="value">The decoded integer value.</param>
    /// <param name="bytesConsumed">The number of source bytes consumed during decoding.</param>
    public HypertextTransferProtocolVersion2HpackIntegerDecodeResult(int value, int bytesConsumed)
    {
        Value = value;
        BytesConsumed = bytesConsumed;
    }
}
