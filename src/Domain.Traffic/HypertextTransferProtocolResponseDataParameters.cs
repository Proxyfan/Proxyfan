using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Defines the values required to create an immutable <see cref="HypertextTransferProtocolResponseData" /> instance.
/// </summary>
public sealed class HypertextTransferProtocolResponseDataParameters
{
    /// <summary>
    ///     Gets the response body bytes.
    /// </summary>
    public required ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>
    ///     Gets the response headers.
    /// </summary>
    public required HeaderCollection Headers { get; init; }

    /// <summary>
    ///     Gets the HTTP reason phrase.
    /// </summary>
    public required string ReasonPhrase { get; init; }

    /// <summary>
    ///     Gets the HTTP status code.
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    ///     Gets the HTTP version string.
    /// </summary>
    public required string Version { get; init; }
}