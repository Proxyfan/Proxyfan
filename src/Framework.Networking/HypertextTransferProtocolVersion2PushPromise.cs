using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Result of parsing the payload of an HTTP/2 PUSH_PROMISE frame (RFC 7540 § 6.6).
/// </summary>
public readonly record struct HypertextTransferProtocolVersion2PushPromise
{
    /// <summary>
    ///     Gets the header block fragment carried by the frame. Concatenate with any subsequent
    ///     CONTINUATION fragments (via the assembler) before HPACK-decoding.
    /// </summary>
    public ReadOnlyMemory<byte> HeaderBlockFragment { get; }

    /// <summary>
    ///     Gets the promised stream identifier — the server-initiated even-numbered stream id
    ///     on which the pushed response will be delivered.
    /// </summary>
    public uint PromisedStreamIdentifier { get; }

    /// <summary>
    ///     Initializes a new push-promise result.
    /// </summary>
    /// <param name="promisedStreamIdentifier">The promised stream identifier.</param>
    /// <param name="headerBlockFragment">The header block fragment.</param>
    public HypertextTransferProtocolVersion2PushPromise(uint promisedStreamIdentifier, ReadOnlyMemory<byte> headerBlockFragment)
    {
        PromisedStreamIdentifier = promisedStreamIdentifier;
        HeaderBlockFragment = headerBlockFragment;
    }
}
