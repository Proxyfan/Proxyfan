using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Represents the parsed header section of an HTTP response read from a
///     <see cref="System.IO.Pipelines.PipeReader" />. Returned by
///     <see cref="HypertextTransferProtocolPipeHelpers.ReadResponseHeadersAsync(System.IO.Pipelines.PipeReader, int, System.Threading.CancellationToken)" />
///     when the caller wants to inspect the response before deciding whether to read the body
///     normally or to switch to a streaming relay (e.g. for Server-Sent Events).
/// </summary>
public sealed class HypertextTransferProtocolResponseHeaderRead
{
    /// <summary>
    ///     Gets the verbatim response header bytes (status line + headers + trailing
    ///     <c>\r\n\r\n</c>) as they appeared on the wire.
    /// </summary>
    public required byte[] HeaderBytes { get; init; }

    /// <summary>
    ///     Gets the parsed response data with an empty body. The body must be read separately
    ///     via
    ///     <see cref="HypertextTransferProtocolPipeHelpers.ReadResponseBodyAsync(System.IO.Pipelines.PipeReader, HypertextTransferProtocolResponseHeaderRead, string, System.Threading.CancellationToken)" />
    ///     or streamed by a specialised handler.
    /// </summary>
    public required HypertextTransferProtocolResponseData Response { get; init; }
}
