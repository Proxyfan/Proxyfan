using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Represents the parsed start line of an HTTP request.
/// </summary>
public sealed class HypertextTransferProtocolRequestLine
{
    /// <summary>
    ///     Gets the HTTP method.
    /// </summary>
    public string Method { get; }

    /// <summary>
    ///     Gets the request target URI.
    /// </summary>
    public Uri RequestTargetUri { get; }

    /// <summary>
    ///     Gets the HTTP version string.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolRequestLine" /> instance.
    /// </summary>
    /// <param name="method">
    ///     The HTTP method.
    /// </param>
    /// <param name="requestTargetUri">
    ///     The request target URI.
    /// </param>
    /// <param name="version">
    ///     The HTTP version string.
    /// </param>
    public HypertextTransferProtocolRequestLine(string method, Uri requestTargetUri, string version)
    {
        Method = method;
        RequestTargetUri = requestTargetUri;
        Version = version;
    }
}