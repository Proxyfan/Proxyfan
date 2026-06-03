using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Parses a minimal `key=value` text configuration file into a
///     <see cref="ConfigurationSnapshot" />. Lines starting with `#` are treated as comments.
///     Empty lines are skipped. Malformed lines are reported as diagnostics.
/// </summary>
public static class KeyValueConfigurationParser
{
    /// <summary>
    ///     Parses the supplied configuration text into a snapshot.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>The parsed snapshot and parse diagnostics.</returns>
    public static KeyValueConfigurationParseResult Parse(string text)
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
            if (separatorIndex < 0)
            {
                malformedLineNumbers.Add(lineNumber);
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            if (key.Length == 0)
            {
                malformedLineNumbers.Add(lineNumber);
                continue;
            }

            values[key] = value;
        }

        var snapshot = new ConfigurationSnapshot(values);
        return new KeyValueConfigurationParseResult
        {
            MalformedLineNumbers = malformedLineNumbers,
            Snapshot = snapshot,
        };
    }
}
