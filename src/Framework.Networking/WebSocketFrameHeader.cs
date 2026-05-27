namespace Proxyfan.Framework.Networking;

/// <summary>
///     Helper carrying the parsed metadata of a WebSocket frame header.
/// </summary>
public sealed class WebSocketFrameHeader
{
    /// <summary>
    ///     Gets the total header length in bytes (including extended length and masking key).
    /// </summary>
    public int HeaderLength { get; }

    /// <summary>
    ///     Gets the masking key for client-to-server frames, or null when unmasked.
    /// </summary>
    public byte[]? MaskingKey { get; }

    /// <summary>
    ///     Gets the payload length in bytes.
    /// </summary>
    public long PayloadLength { get; }

    /// <summary>
    ///     Initializes a new <see cref="WebSocketFrameHeader" />.
    /// </summary>
    /// <param name="headerLength">The total header length in bytes.</param>
    /// <param name="payloadLength">The payload length in bytes.</param>
    /// <param name="maskingKey">The masking key, or null when unmasked.</param>
    public WebSocketFrameHeader(int headerLength, long payloadLength, byte[]? maskingKey)
    {
        HeaderLength = headerLength;
        PayloadLength = payloadLength;
        MaskingKey = maskingKey;
    }
}
