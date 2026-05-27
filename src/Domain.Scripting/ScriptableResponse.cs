using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Mutable view of an HTTP response exposed to user scripts. Mutations made through this
///     object are applied to the outgoing response by the scripting engine.
/// </summary>
public sealed class ScriptableResponse
{
    /// <summary>
    ///     Gets the mutable header collection (initially a snapshot of the source headers).
    /// </summary>
    public ScriptableHeaders Headers { get; }

    /// <summary>
    ///     Gets or sets the response reason phrase.
    /// </summary>
    public string ReasonPhrase { get; set; }

    /// <summary>
    ///     Gets or sets the status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    ///     Initializes a new <see cref="ScriptableResponse" /> from the supplied source response.
    /// </summary>
    /// <param name="source">The original captured response.</param>
    public ScriptableResponse(HypertextTransferProtocolResponseData source)
    {
        StatusCode = source.StatusCode;
        ReasonPhrase = source.ReasonPhrase;
        var headers = new ScriptableHeaders();
        foreach (var header in source.Headers)
        {
            if (header.Value.Length > 0)
            {
                headers.Set(header.Key, header.Value[0]);
            }
        }

        Headers = headers;
    }
}
