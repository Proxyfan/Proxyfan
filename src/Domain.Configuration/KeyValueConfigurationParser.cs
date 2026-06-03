using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Parses a minimal `key=value` text configuration file into a
///     <see cref="ConfigurationParseResult" />. Lines starting with `#` are treated as
///     comments. Empty lines are skipped. Lines that are neither empty, nor a comment, nor
///     a valid <c>key=value</c> pair are recorded as malformed in the returned result
///     instead of being silently discarded.
/// </summary>
public static class KeyValueConfigurationParser
{
    /// <summary>
    ///     Parses the supplied configuration text and returns both the successfully parsed
    ///     snapshot and the 1-based line numbers of any malformed lines encountered.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>
    ///     A <see cref="ConfigurationParseResult" /> containing the snapshot of valid
    ///     key-value pairs and the line numbers of any malformed lines.
    /// </returns>
    public static ConfigurationParseResult Parse(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var malformedLineNumbers = new List<int>();
        using var reader = new StringReader(text);
        string? line;
        var lineNumber = 0;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                malformedLineNumbers.Add(lineNumber);
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            values[key] = value;
        }

        var snapshot = new ConfigurationSnapshot(values);
        var result = new ConfigurationParseResult
        {
            MalformedLineNumbers = malformedLineNumbers,
            Snapshot = snapshot,
        };
        return result;
    }
}
