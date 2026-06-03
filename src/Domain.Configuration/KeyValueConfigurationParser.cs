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
    ///     Parses the supplied configuration text into a snapshot, discarding any malformed-line
    ///     diagnostics. Call <see cref="ParseWithDiagnostics" /> when diagnostics are required.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>The parsed snapshot.</returns>
    public static ConfigurationSnapshot Parse(string text)
    {
        var result = ParseWithDiagnostics(text);
        return result.Snapshot;
    }

    /// <summary>
    ///     Parses the supplied configuration text into a snapshot and diagnostics.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>The parsed snapshot and malformed-line diagnostics.</returns>
    public static KeyValueConfigurationParseResult ParseWithDiagnostics(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var diagnostics = new List<KeyValueConfigurationParseDiagnostic>();
        using var reader = new StringReader(text);
        var lineNumber = 0;
        string? line;
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
                var diagnostic = new KeyValueConfigurationParseDiagnostic(
                    lineNumber,
                    line,
                    "Configuration line must be in key=value format.");
                diagnostics.Add(diagnostic);
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
        var result = new KeyValueConfigurationParseResult
        {
            Diagnostics = diagnostics,
            Snapshot = snapshot,
        };
        return result;
    }
}
