using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents immutable HTTP request data captured for a traffic flow.
/// </summary>
public sealed class HypertextTransferProtocolRequestData
{
    /// <summary>
    ///     Gets the request body bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    ///     Gets the request headers.
    /// </summary>
    public HeaderCollection Headers { get; }

    /// <summary>
    ///     Gets the HTTP method.
    /// </summary>
    public string Method { get; }

    /// <summary>
    ///     Gets the request URI.
    /// </summary>
    public Uri RequestUri { get; }

    /// <summary>
    ///     Gets the HTTP version string.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolRequestData" /> instance.
    /// </summary>
    /// <param name="parameters">
    ///     The values used to populate the request.
    /// </param>
    public HypertextTransferProtocolRequestData(HypertextTransferProtocolRequestDataParameters parameters)
    {
        Body = parameters.Body;
        Headers = parameters.Headers;
        Method = parameters.Method;
        RequestUri = parameters.RequestUri;
        Version = parameters.Version;
    }
}