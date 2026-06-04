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
    ///     Renders the request as a cURL command line using the platform-appropriate shell
    ///     quoting strategy. Binary bodies are not supported and the body is included verbatim
    ///     assuming UTF-8.
    /// </summary>
    /// <param name="request">The captured request.</param>
    /// <returns>A cURL command line.</returns>
    public static string ToCurl(HypertextTransferProtocolRequestData request)
    {
        var shellFlavor = OperatingSystem.IsWindows()
            ? CurlCommandShellFlavor.PowerShell
            : CurlCommandShellFlavor.Bash;
        return ToCurl(request, shellFlavor);
    }

    /// <summary>
    ///     Renders the request as a cURL command line using the specified shell quoting strategy.
    ///     Binary bodies are not supported and the body is included verbatim assuming UTF-8.
    /// </summary>
    /// <param name="request">The captured request.</param>
    /// <param name="shellFlavor">The shell flavor used for argument quoting.</param>
    /// <returns>A cURL command line.</returns>
    public static string ToCurl(HypertextTransferProtocolRequestData request, CurlCommandShellFlavor shellFlavor)
    {
        var builder = new StringBuilder();
        builder.Append("curl -X ").Append(QuoteArgument(request.Method, shellFlavor));
        builder.Append(' ').Append(QuoteArgument(request.RequestUri.ToString(), shellFlavor));

        foreach (var header in request.Headers)
        {
            for (var index = 0; index < header.Value.Length; index++)
            {
                builder.Append(" -H ").Append(QuoteArgument(header.Key + ": " + header.Value[index], shellFlavor));
            }
        }

        if (request.Body.Length > 0)
        {
            var bodyText = Encoding.UTF8.GetString(request.Body.Span);
            builder.Append(" --data ").Append(QuoteArgument(bodyText, shellFlavor));
        }

        return builder.ToString();
    }

    private static string QuoteArgument(string value, CurlCommandShellFlavor shellFlavor)
    {
        var escapedValue = shellFlavor switch
        {
            CurlCommandShellFlavor.Bash => value.Replace("'", "'\\''", StringComparison.Ordinal),
            CurlCommandShellFlavor.PowerShell => value.Replace("'", "''", StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(shellFlavor), shellFlavor, "Unsupported cURL shell flavor."),
        };
        return "'" + escapedValue + "'";
    }
}
