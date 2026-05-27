using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Editable builder used by the Request Composer feature to construct a new request from
///     scratch or by cloning a captured request. Build calls produce immutable
///     <see cref="HypertextTransferProtocolRequestData" /> instances ready to forward.
/// </summary>
public sealed class HypertextTransferProtocolRequestComposer
{
    private readonly Dictionary<string, string> _headers;

    /// <summary>
    ///     Gets or sets the request body bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; set; }

    /// <summary>
    ///     Gets or sets the HTTP method (uppercase, e.g. <c>"GET"</c>).
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    ///     Gets or sets the request URI.
    /// </summary>
    public Uri? RequestUri { get; set; }

    /// <summary>
    ///     Gets or sets the HTTP protocol version (e.g. <c>"HTTP/1.1"</c>).
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolRequestComposer" /> with a
    ///     default GET, HTTP/1.1 request and no headers or body.
    /// </summary>
    public HypertextTransferProtocolRequestComposer()
    {
        Method = "GET";
        Version = "HTTP/1.1";
        Body = ReadOnlyMemory<byte>.Empty;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _headers = headers;
    }

    /// <summary>
    ///     Initializes a composer pre-populated from an existing captured request.
    /// </summary>
    /// <param name="source">The captured request to clone.</param>
    public HypertextTransferProtocolRequestComposer(HypertextTransferProtocolRequestData source)
        : this()
    {
        Method = source.Method;
        RequestUri = source.RequestUri;
        Version = source.Version;
        Body = source.Body;
        foreach (var header in source.Headers)
        {
            if (header.Value.Length > 0)
            {
                _headers[header.Key] = header.Value[0];
            }
        }
    }

    /// <summary>
    ///     Builds the composed request into an immutable
    ///     <see cref="HypertextTransferProtocolRequestData" />.
    /// </summary>
    /// <returns>The composed request data.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the request URI has not been supplied or the method is blank.
    /// </exception>
    public HypertextTransferProtocolRequestData Build()
    {
        if (RequestUri is null)
        {
            throw new InvalidOperationException("RequestUri must be supplied before building.");
        }

        if (string.IsNullOrWhiteSpace(Method))
        {
            throw new InvalidOperationException("Method must be supplied before building.");
        }

        var headers = HeaderCollection.Empty;
        foreach (var header in _headers)
        {
            headers = headers.Add(header.Key, header.Value);
        }

        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Body,
            Headers = headers,
            Method = Method,
            RequestUri = RequestUri,
            Version = Version,
        };
        var built = new HypertextTransferProtocolRequestData(parameters);
        return built;
    }

    /// <summary>
    ///     Removes the header with the supplied name. Returns true when removed.
    /// </summary>
    /// <param name="name">The header name (case-insensitive).</param>
    /// <returns><see langword="true" /> when the header was removed.</returns>
    public bool HasRemoved(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _headers.Remove(name);
    }

    /// <summary>
    ///     Sets a header value, replacing any existing value for the same name.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    public void SetHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _headers[name] = value;
    }
}
