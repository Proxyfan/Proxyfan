namespace Proxyfan.Framework.Networking;

/// <summary>
///     The body framing strategy required to correctly delimit an HTTP/1.x message body
///     per RFC 7230 § 3.3.3.
/// </summary>
public enum HypertextTransferProtocolBodyFraming
{
    /// <summary>
    ///     The message has no body. This applies to HEAD responses, 1xx, 204, and 304
    ///     responses, and to requests without <c>Content-Length</c> or <c>Transfer-Encoding</c>.
    /// </summary>
    None,

    /// <summary>
    ///     The body is framed using chunked transfer-coding (<c>Transfer-Encoding: chunked</c>).
    /// </summary>
    Chunked,

    /// <summary>
    ///     The body length is given by a <c>Content-Length</c> header.
    /// </summary>
    ContentLength,

    /// <summary>
    ///     The body extends until the server closes the connection. Only valid for responses
    ///     where neither <c>Transfer-Encoding</c> nor <c>Content-Length</c> is present.
    /// </summary>
    UntilClose,
}
