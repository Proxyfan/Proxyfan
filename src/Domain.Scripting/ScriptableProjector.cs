using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Helpers that materialize immutable
///     <see cref="HypertextTransferProtocolRequestData" /> / <see cref="HypertextTransferProtocolResponseData" />
///     instances from the mutable script views.
/// </summary>
public static class ScriptableProjector
{
    /// <summary>
    ///     Materializes an immutable request from the supplied script view, copying body bytes
    ///     from the supplied source request unchanged.
    /// </summary>
    /// <param name="view">The mutated script view.</param>
    /// <param name="source">The source request whose body and version are reused.</param>
    /// <returns>An immutable request representing the post-script state.</returns>
    public static HypertextTransferProtocolRequestData Project(
        ScriptableRequest view,
        HypertextTransferProtocolRequestData source)
    {
        var headers = HeaderCollection.Empty;
        foreach (var header in view.Headers.Enumerate())
        {
            headers = headers.Add(header.Key, header.Value);
        }

        var requestUri = new Uri(view.Url);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = source.Body,
            Headers = headers,
            Method = view.Method,
            RequestUri = requestUri,
            Version = source.Version,
        };
        var data = new HypertextTransferProtocolRequestData(parameters);
        return data;
    }

    /// <summary>
    ///     Materializes an immutable response from the supplied script view, copying body bytes
    ///     and version from the supplied source response unchanged.
    /// </summary>
    /// <param name="view">The mutated script view.</param>
    /// <param name="source">The source response whose body is reused.</param>
    /// <returns>An immutable response representing the post-script state.</returns>
    public static HypertextTransferProtocolResponseData Project(
        ScriptableResponse view,
        HypertextTransferProtocolResponseData source)
    {
        var headers = HeaderCollection.Empty;
        foreach (var header in view.Headers.Enumerate())
        {
            headers = headers.Add(header.Key, header.Value);
        }

        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = source.Body,
            Headers = headers,
            ReasonPhrase = view.ReasonPhrase,
            StatusCode = view.StatusCode,
            Version = source.Version,
        };
        var data = new HypertextTransferProtocolResponseData(parameters);
        return data;
    }
}
