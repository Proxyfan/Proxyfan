using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Represents a parsed HTTP proxy request together with its raw wire bytes.
/// </summary>
public sealed class HypertextTransferProtocolProxyRequestExchange
{
    /// <summary>
    ///     Gets the request body bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    ///     Gets the raw request header bytes.
    /// </summary>
    public byte[] HeaderBytes { get; }

    /// <summary>
    ///     Gets the parsed request data.
    /// </summary>
    public HypertextTransferProtocolRequestData Request { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolProxyRequestExchange" /> instance.
    /// </summary>
    /// <param name="body">
    ///     The request body bytes.
    /// </param>
    /// <param name="headerBytes">
    ///     The raw request header bytes.
    /// </param>
    /// <param name="request">
    ///     The parsed request data.
    /// </param>
    public HypertextTransferProtocolProxyRequestExchange(
        ReadOnlyMemory<byte> body,
        byte[] headerBytes,
        HypertextTransferProtocolRequestData request)
    {
        Body = body;
        HeaderBytes = headerBytes;
        Request = request;
    }
}