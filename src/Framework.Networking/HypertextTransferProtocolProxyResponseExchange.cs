using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Represents a parsed HTTP proxy response together with its raw wire bytes.
/// </summary>
public sealed class HypertextTransferProtocolProxyResponseExchange
{
    /// <summary>
    ///     Gets the response body bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    ///     Gets the raw response header bytes.
    /// </summary>
    public byte[] HeaderBytes { get; }

    /// <summary>
    ///     Gets the parsed response data.
    /// </summary>
    public HypertextTransferProtocolResponseData Response { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolProxyResponseExchange" /> instance.
    /// </summary>
    /// <param name="body">
    ///     The response body bytes.
    /// </param>
    /// <param name="headerBytes">
    ///     The raw response header bytes.
    /// </param>
    /// <param name="response">
    ///     The parsed response data.
    /// </param>
    public HypertextTransferProtocolProxyResponseExchange(
        ReadOnlyMemory<byte> body,
        byte[] headerBytes,
        HypertextTransferProtocolResponseData response)
    {
        Body = body;
        HeaderBytes = headerBytes;
        Response = response;
    }
}