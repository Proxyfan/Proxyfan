using System.Text;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Serializes captured HTTP request and response data back into the canonical raw HTTP
///     message text (start line, header block, body) for the Raw inspector tab. Binary
///     bodies are rendered as UTF-8 with replacement characters where bytes are not valid.
/// </summary>
public static class RawHypertextTransferProtocolMessageFormatter
{
    /// <summary>
    ///     Formats the supplied request as raw HTTP text. Returns an empty string when the
    ///     request is null.
    /// </summary>
    /// <param name="request">The request to format.</param>
    /// <returns>The raw message text.</returns>
    public static string FormatRequest(HypertextTransferProtocolRequestData? request)
    {
        if (request is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append(request.Method);
        builder.Append(' ');
        builder.Append(request.RequestUri);
        builder.Append(' ');
        builder.AppendLine(request.Version);
        AppendHeaders(builder, request.Headers);
        builder.AppendLine();

        if (!request.Body.IsEmpty)
        {
            builder.Append(Encoding.UTF8.GetString(request.Body.Span));
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Formats the supplied response as raw HTTP text. Returns an empty string when the
    ///     response is null.
    /// </summary>
    /// <param name="response">The response to format.</param>
    /// <returns>The raw message text.</returns>
    public static string FormatResponse(HypertextTransferProtocolResponseData? response)
    {
        if (response is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append(response.Version);
        builder.Append(' ');
        builder.Append(response.StatusCode);
        builder.Append(' ');
        builder.AppendLine(response.ReasonPhrase);
        AppendHeaders(builder, response.Headers);
        builder.AppendLine();

        if (!response.Body.IsEmpty)
        {
            builder.Append(Encoding.UTF8.GetString(response.Body.Span));
        }

        return builder.ToString();
    }

    private static void AppendHeaders(StringBuilder builder, HeaderCollection headers)
    {
        foreach (var header in headers)
        {
            foreach (var value in header.Value)
            {
                builder.Append(header.Key);
                builder.Append(": ");
                builder.AppendLine(value);
            }
        }
    }
}
