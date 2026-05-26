using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents immutable HTTP response data captured for a traffic flow.
/// </summary>
public sealed class HypertextTransferProtocolResponseData
{
    /// <summary>
    ///     Gets the response body bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    ///     Gets the response headers.
    /// </summary>
    public HeaderCollection Headers { get; }

    /// <summary>
    ///     Gets the HTTP reason phrase.
    /// </summary>
    public string ReasonPhrase { get; }

    /// <summary>
    ///     Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    ///     Gets the HTTP version string.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolResponseData" /> instance.
    /// </summary>
    /// <param name="parameters">
    ///     The values used to populate the response.
    /// </param>
    public HypertextTransferProtocolResponseData(HypertextTransferProtocolResponseDataParameters parameters)
    {
        Body = parameters.Body;
        Headers = parameters.Headers;
        ReasonPhrase = parameters.ReasonPhrase;
        StatusCode = parameters.StatusCode;
        Version = parameters.Version;
    }
}