using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Mutable view of an HTTP request exposed to user scripts. Mutations made through this
///     object are applied to the outgoing request by the scripting engine.
/// </summary>
public sealed class ScriptableRequest
{
    /// <summary>
    ///     Gets the mutable header collection (initially a snapshot of the source headers).
    /// </summary>
    public ScriptableHeaders Headers { get; }

    /// <summary>
    ///     Gets or sets the request method.
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    ///     Gets or sets the request URI as a string.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    ///     Initializes a new <see cref="ScriptableRequest" /> from the supplied source request.
    /// </summary>
    /// <param name="source">The original captured request.</param>
    public ScriptableRequest(HypertextTransferProtocolRequestData source)
    {
        Method = source.Method;
        Url = source.RequestUri.ToString();
        var headers = new ScriptableHeaders();
        foreach (var header in source.Headers)
        {
            foreach (var value in header.Value)
            {
                headers.Add(header.Key, value);
            }
        }

        Headers = headers;
    }
}
