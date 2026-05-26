namespace Proxyfan.Framework.Networking;

/// <summary>
///     Represents the parsed status line of an HTTP response.
/// </summary>
public sealed class HypertextTransferProtocolResponseStatusLine
{
    /// <summary>
    ///     Gets the reason phrase.
    /// </summary>
    public string ReasonPhrase { get; }

    /// <summary>
    ///     Gets the numeric status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    ///     Gets the HTTP version string.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolResponseStatusLine" /> instance.
    /// </summary>
    /// <param name="statusCode">
    ///     The numeric status code.
    /// </param>
    /// <param name="reasonPhrase">
    ///     The reason phrase.
    /// </param>
    /// <param name="version">
    ///     The HTTP version string.
    /// </param>
    public HypertextTransferProtocolResponseStatusLine(int statusCode, string reasonPhrase, string version)
    {
        ReasonPhrase = reasonPhrase;
        StatusCode = statusCode;
        Version = version;
    }
}