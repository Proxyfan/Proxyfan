using System;
using System.Collections.Generic;

namespace Proxyfan.Cli;

/// <summary>
///     Parses the arguments of the <c>send</c> CLI command into a <see cref="CliSendRequest" />.
/// </summary>
public static class CliSendArgumentParser
{
    /// <summary>
    ///     Parses send-command arguments. Returns <see langword="null" /> when the URL or
    ///     method cannot be determined.
    /// </summary>
    /// <param name="args">The full argument array, including the leading <c>send</c> verb.</param>
    /// <returns>The parsed request, or null.</returns>
    public static CliSendRequest? Parse(string[] args)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string method = "GET";
        string? url = null;
        string? body = null;
        var index = 1;

        while (index < args.Length)
        {
            var token = args[index];
            var hasNextArg = index + 1 < args.Length;

            if (string.Equals(token, "--method", StringComparison.OrdinalIgnoreCase) && hasNextArg)
            {
                method = args[index + 1].ToUpperInvariant();
                index += 2;
            }
            else if (string.Equals(token, "--url", StringComparison.OrdinalIgnoreCase) && hasNextArg)
            {
                url = args[index + 1];
                index += 2;
            }
            else if (string.Equals(token, "--header", StringComparison.OrdinalIgnoreCase) && hasNextArg)
            {
                AddHeader(args[index + 1], headers);
                index += 2;
            }
            else if (string.Equals(token, "--body", StringComparison.OrdinalIgnoreCase) && hasNextArg)
            {
                body = args[index + 1];
                index += 2;
            }
            else
            {
                index += 1;
            }
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var request = new CliSendRequest(method, url, headers, body);
        return request;
    }

    private static void AddHeader(string raw, Dictionary<string, string> headers)
    {
        var separatorIndex = raw.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return;
        }

        var name = raw[..separatorIndex].Trim();
        var value = raw[(separatorIndex + 1)..].TrimStart();
        if (name.Length > 0)
        {
            headers[name] = value;
        }
    }
}
