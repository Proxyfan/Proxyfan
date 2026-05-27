using System.Collections.Generic;

namespace Proxyfan.Cli;

/// <summary>
///     Arguments to the Send command: method, URL, optional headers, and optional body.
/// </summary>
public sealed class CliSendRequest
{
    /// <summary>
    ///     Gets the request body (UTF-8 string), or <see langword="null" /> when no body was supplied.
    /// </summary>
    public string? Body { get; }

    /// <summary>
    ///     Gets the parsed header values keyed by header name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    ///     Gets the HTTP method (uppercase, e.g. <c>"GET"</c>).
    /// </summary>
    public string Method { get; }

    /// <summary>
    ///     Gets the absolute target URL.
    /// </summary>
    public string Url { get; }

    /// <summary>
    ///     Initializes a new <see cref="CliSendRequest" />.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="url">The target URL.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="body">The body, or null.</param>
    public CliSendRequest(string method, string url, IReadOnlyDictionary<string, string> headers, string? body)
    {
        Method = method;
        Url = url;
        Headers = headers;
        Body = body;
    }
}
