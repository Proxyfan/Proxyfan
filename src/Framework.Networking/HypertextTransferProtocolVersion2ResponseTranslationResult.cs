using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     The result of translating an HTTP/1.1 response into the HTTP/2 wire shape — an ordered
///     header list (with <c>:status</c> first) and an opaque body view that the encoder pipes
///     into DATA frames.
/// </summary>
public sealed class HypertextTransferProtocolVersion2ResponseTranslationResult
{
    /// <summary>
    ///     Gets the body bytes (unchanged from the source HTTP/1.1 response).
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    ///     Gets the translated HTTP/2 header list. The first entry is always <c>:status</c>.
    /// </summary>
    public IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> Headers { get; }

    /// <summary>
    ///     Initializes a new translation result.
    /// </summary>
    /// <param name="headers">The translated headers (first entry must be <c>:status</c>).</param>
    /// <param name="body">The opaque body bytes.</param>
    public HypertextTransferProtocolVersion2ResponseTranslationResult(
        IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> headers,
        ReadOnlyMemory<byte> body)
    {
        Headers = headers;
        Body = body;
    }
}
