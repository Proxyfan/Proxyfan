using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Parses a minimal <c>key=value</c> text configuration file into a
///     <see cref="ConfigurationParseResult" />. Lines starting with <c>#</c> are treated
///     as comments. Empty lines are skipped. Lines that are non-empty, non-comment, and
///     lack a valid <c>=</c> separator are reported as
///     <see cref="ConfigurationParseDiagnostic" /> entries rather than silently discarded.
/// </summary>
public static class KeyValueConfigurationParser
{
    /// <summary>
    ///     Parses the supplied configuration text and returns a result containing the
    ///     successfully parsed snapshot together with diagnostics for any malformed lines.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>
    ///     A <see cref="ConfigurationParseResult" /> whose
    ///     <see cref="ConfigurationParseResult.Snapshot" /> holds the valid key-value pairs
    ///     and whose <see cref="ConfigurationParseResult.MalformedLines" /> lists every line
    ///     that could not be parsed.
    /// </returns>
    public static ConfigurationParseResult Parse(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var malformed = new List<ConfigurationParseDiagnostic>();
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
                var diagnostic = new ConfigurationParseDiagnostic
                {
                    LineContent = line,
                    LineNumber = lineNumber,
                };
                malformed.Add(diagnostic);
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
        var result = new ConfigurationParseResult
        {
            MalformedLines = malformed,
            Snapshot = snapshot,
        };
        return result;
    }
}
