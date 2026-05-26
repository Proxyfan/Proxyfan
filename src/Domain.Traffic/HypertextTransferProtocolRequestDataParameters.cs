using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Defines the values required to create an immutable <see cref="HypertextTransferProtocolRequestData" /> instance.
/// </summary>
public sealed class HypertextTransferProtocolRequestDataParameters
{
    /// <summary>
    ///     Gets the request body bytes.
    /// </summary>
    public required ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>
    ///     Gets the request headers.
    /// </summary>
    public required HeaderCollection Headers { get; init; }

    /// <summary>
    ///     Gets the HTTP method.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    ///     Gets the request URI.
    /// </summary>
    public required Uri RequestUri { get; init; }

    /// <summary>
    ///     Gets the HTTP version string.
    /// </summary>
    public required string Version { get; init; }
}