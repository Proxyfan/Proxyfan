using System;
using System.Text;

namespace Proxyfan.Domain.Composer;

/// <summary>
///     Converts a <see cref="ComposerRequest" /> to a cURL command-line string suitable for
///     copying into a terminal or sharing in documentation.
/// </summary>
public static class CurlExporter
{
    /// <summary>
    ///     Returns a cURL command equivalent to the supplied composer request. Uses POSIX
    ///     single-quote escaping for header and body arguments so the output is portable to
    ///     bash, zsh and macOS/Linux terminals.
    /// </summary>
    /// <param name="request">The request to export.</param>
    /// <returns>The cURL command string.</returns>
    public static string ToCurl(ComposerRequest request)
    {
        var builder = new StringBuilder();
        builder.Append("curl");

        if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            builder.Append(" -X ");
            builder.Append(request.Method);
        }

        for (var index = 0; index < request.Headers.Count; index++)
        {
            var header = request.Headers[index];
            builder.Append(" -H ");
            var headerLine = $"{header.Name}: {header.Value}";
            builder.Append(QuotePosix(headerLine));
        }

        if (request.Body.Count > 0)
        {
            builder.Append(" --data-binary ");
            var bodyText = TryDecodeUtf8(request.Body);
            if (bodyText is not null)
            {
                builder.Append(QuotePosix(bodyText));
            }
            else
            {
                builder.Append(QuotePosix($"@<binary {request.Body.Count} bytes>"));
            }
        }

        builder.Append(' ');
        builder.Append(QuotePosix(request.Url));
        return builder.ToString();
    }

    private static string QuotePosix(string value)
    {
        var escaped = value.Replace("'", "'\\''", StringComparison.Ordinal);
        return $"'{escaped}'";
    }

    private static string? TryDecodeUtf8(System.Collections.Generic.IReadOnlyList<byte> body)
    {
        var bytes = new byte[body.Count];
        for (var index = 0; index < body.Count; index++)
        {
            bytes[index] = body[index];
        }
        try
        {
            var decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var text = decoder.GetString(bytes);
            return text;
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }
}
