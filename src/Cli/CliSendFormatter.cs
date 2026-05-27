using System;
using System.Text;

namespace Proxyfan.Cli;

/// <summary>
///     Formats a <see cref="CliSendRequest" /> as a wire-format HTTP/1.1 message suitable
///     for printing to the terminal.
/// </summary>
public static class CliSendFormatter
{
    /// <summary>
    ///     Formats the supplied request.
    /// </summary>
    /// <param name="request">The request to format.</param>
    /// <returns>The formatted text (with CRLF line endings).</returns>
    public static string Format(CliSendRequest request)
    {
        var builder = new StringBuilder();
        var uri = new Uri(request.Url, UriKind.Absolute);
        var pathAndQuery = uri.PathAndQuery;
        builder.Append(request.Method).Append(' ').Append(pathAndQuery).Append(" HTTP/1.1\r\n");
        builder.Append("Host: ").Append(uri.Host);
        if (!uri.IsDefaultPort)
        {
            builder.Append(':').Append(uri.Port);
        }
        builder.Append("\r\n");

        foreach (var header in request.Headers)
        {
            builder.Append(header.Key).Append(": ").Append(header.Value).Append("\r\n");
        }

        builder.Append("\r\n");

        if (request.Body is not null)
        {
            builder.Append(request.Body);
        }

        return builder.ToString();
    }
}
