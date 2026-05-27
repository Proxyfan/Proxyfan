using System;
using System.Text;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Converts a captured <see cref="HypertextTransferProtocolRequestData" /> into a
///     reproducible cURL command line — a feature universally expected of HTTP debugging tools.
/// </summary>
public static class CurlCommandConverter
{
    /// <summary>
    ///     Renders the request as a cURL command line. Headers are quoted with double quotes;
    ///     binary bodies are not supported and the body is included verbatim assuming UTF-8.
    /// </summary>
    /// <param name="request">The captured request.</param>
    /// <returns>A cURL command line.</returns>
    public static string ToCurl(HypertextTransferProtocolRequestData request)
    {
        var builder = new StringBuilder();
        builder.Append("curl -X ").Append(request.Method);
        builder.Append(" \"").Append(request.RequestUri).Append('"');

        foreach (var header in request.Headers)
        {
            for (var index = 0; index < header.Value.Length; index++)
            {
                var escapedValue = header.Value[index].Replace("\"", "\\\"", StringComparison.Ordinal);
                builder.Append(" -H \"").Append(header.Key).Append(": ").Append(escapedValue).Append('"');
            }
        }

        if (request.Body.Length > 0)
        {
            var bodyText = Encoding.UTF8.GetString(request.Body.Span);
            var escapedBody = bodyText.Replace("'", "'\\''", StringComparison.Ordinal);
            builder.Append(" --data '").Append(escapedBody).Append('\'');
        }

        return builder.ToString();
    }
}
