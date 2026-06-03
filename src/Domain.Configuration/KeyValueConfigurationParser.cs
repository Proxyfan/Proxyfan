using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Parses a minimal `key=value` text configuration file into a
///     <see cref="ConfigurationSnapshot" />. Lines starting with `#` are treated as comments.
///     Empty lines are skipped.
/// </summary>
public static class KeyValueConfigurationParser
{
    /// <summary>
    ///     Parses the supplied configuration text into a snapshot.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>The parsed snapshot.</returns>
    public static ConfigurationSnapshot Parse(string text)
    {
        var parseResult = ParseWithDiagnostics(text);
        return parseResult.Snapshot;
    }

    /// <summary>
    ///     Parses the supplied configuration text into a snapshot and parse diagnostics.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>The parsed snapshot and any malformed-line diagnostics.</returns>
    public static KeyValueConfigurationParseResult ParseWithDiagnostics(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var diagnostics = new List<string>();
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
                diagnostics.Add($"Line {lineNumber} is malformed: '{line}'.");
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            if (key.Length > 0)
            {
                values[key] = value;
            }
        }

        var snapshot = new ConfigurationSnapshot(values);
        return new KeyValueConfigurationParseResult
        {
            Diagnostics = diagnostics,
            Snapshot = snapshot,
        };
    }
}
