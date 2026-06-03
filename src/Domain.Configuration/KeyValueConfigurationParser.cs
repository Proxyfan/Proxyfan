using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Parses a minimal `key=value` text configuration file into a
///     <see cref="ConfigurationSnapshot" />. Lines starting with `#` are treated as comments.
///     Empty lines are skipped. Any non-empty, non-comment line that is not a valid
///     <c>key=value</c> pair is reported as a parse error.
/// </summary>
public static class KeyValueConfigurationParser
{
    /// <summary>
    ///     Parses the supplied configuration text into a snapshot.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>
    ///     A successful <see cref="Result{T}" /> containing the parsed snapshot when every
    ///     content line is a valid <c>key=value</c> pair, or a failed result carrying a
    ///     <see cref="ConfigurationParseError" /> that lists every malformed line.
    /// </returns>
    public static Result<ConfigurationSnapshot> Parse(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var malformedLines = new List<string>();
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                malformedLines.Add(trimmed);
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            if (key.Length > 0)
            {
                values[key] = value;
            }
        }

        if (malformedLines.Count > 0)
        {
            var parseError = new ConfigurationParseError(malformedLines);
            return Result.Failure<ConfigurationSnapshot>(parseError);
        }

        var snapshot = new ConfigurationSnapshot(values);
        return new Result<ConfigurationSnapshot>(snapshot);
    }
}
