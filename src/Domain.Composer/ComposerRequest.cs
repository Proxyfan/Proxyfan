using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Composer;

/// <summary>
///     A user-composed HTTP request ready for execution. Immutable — modifications produce
///     a new instance through <see cref="ComposerRequestBuilder" />.
/// </summary>
public sealed class ComposerRequest
{
    /// <summary>
    ///     Gets the raw request body bytes (may be empty).
    /// </summary>
    public IReadOnlyList<byte> Body { get; }

    /// <summary>
    ///     Gets the request headers in insertion order.
    /// </summary>
    public IReadOnlyList<ComposerRequestHeader> Headers { get; }

    /// <summary>
    ///     Gets the HTTP method (uppercase, e.g. GET, POST). Defaults to GET.
    /// </summary>
    public string Method { get; }

    /// <summary>
    ///     Gets the absolute request URL.
    /// </summary>
    public string Url { get; }

    /// <summary>
    ///     Initializes a new <see cref="ComposerRequest" />.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="url">The absolute URL.</param>
    /// <param name="headers">The headers (may be empty).</param>
    /// <param name="body">The body bytes (may be empty).</param>
    public ComposerRequest(
        string method,
        string url,
        IReadOnlyList<ComposerRequestHeader> headers,
        IReadOnlyList<byte> body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        Method = method.ToUpperInvariant();
        Url = url;
        Headers = headers;
        Body = body;
    }
}
