using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Parses a minimal `key=value` text configuration file into a
///     <see cref="KeyValueConfigurationParseResult" />. Lines starting with `#` are treated
///     as comments. Empty lines are skipped. Any non-empty, non-comment line that does not
///     contain a valid <c>key=value</c> pair is recorded in
///     <see cref="KeyValueConfigurationParseResult.MalformedLines" /> so that callers can
///     surface diagnostics rather than silently discarding malformed input.
/// </summary>
public static class KeyValueConfigurationParser
{
    /// <summary>
    ///     Parses the supplied configuration text and returns a
    ///     <see cref="KeyValueConfigurationParseResult" /> that includes both the valid
    ///     snapshot and any lines that could not be interpreted as <c>key=value</c> pairs.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>The parse result, including any malformed-line diagnostics.</returns>
    public static KeyValueConfigurationParseResult Parse(string text)
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
            else
            {
                malformedLines.Add(trimmed);
            }
        }

        var snapshot = new ConfigurationSnapshot(values);
        if (malformedLines.Count > 0)
        {
            return KeyValueConfigurationParseResults.Failure(snapshot, malformedLines);
        }

        return KeyValueConfigurationParseResults.Success(snapshot);
    }
}
