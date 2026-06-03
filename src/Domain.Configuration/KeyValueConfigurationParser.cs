using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Parses a minimal `key=value` text configuration file into a
///     <see cref="ConfigurationSnapshot" />. Lines starting with `#` are treated as comments.
///     Empty lines are skipped. Malformed non-empty, non-comment lines are reported as
///     diagnostics in the returned <see cref="KeyValueConfigurationParseResult" />.
/// </summary>
public static class KeyValueConfigurationParser
{
    /// <summary>
    ///     Parses the supplied configuration text into a snapshot and any malformed-line
    ///     diagnostics.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <returns>The parsed snapshot and diagnostics.</returns>
    public static KeyValueConfigurationParseResult Parse(string text)
    {
        var diagnostics = new List<KeyValueConfigurationParseDiagnostic>();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(text);
        string? line;
        var lineNumber = 0;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (CanSkipLine(line))
            {
                continue;
            }

            if (!CanAddValue(values, diagnostics, line, lineNumber))
            {
                continue;
            }
        }

        var snapshot = new ConfigurationSnapshot(values);
        var result = new KeyValueConfigurationParseResult(snapshot, diagnostics);
        return result;
    }

    /// <summary>
    ///     Adds a malformed-line diagnostic for the supplied one-based line number.
    /// </summary>
    /// <param name="diagnostics">The destination collection for malformed-line diagnostics.</param>
    /// <param name="lineNumber">The one-based line number in the source text.</param>
    private static void AddMalformedLineDiagnostic(
        ICollection<KeyValueConfigurationParseDiagnostic> diagnostics,
        int lineNumber)
    {
        var diagnostic = new KeyValueConfigurationParseDiagnostic(
            lineNumber,
            "Expected a non-empty 'key=value' configuration entry.");
        diagnostics.Add(diagnostic);
    }

    /// <summary>
    ///     Parses a non-empty, non-comment line and adds its key/value pair when valid.
    /// </summary>
    /// <param name="values">The destination dictionary for parsed values.</param>
    /// <param name="diagnostics">The destination collection for malformed-line diagnostics.</param>
    /// <param name="line">The raw configuration line.</param>
    /// <param name="lineNumber">The one-based line number in the source text.</param>
    /// <returns>
    ///     <see langword="true" /> when the line was parsed successfully; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    private static bool CanAddValue(
        Dictionary<string, string> values,
        ICollection<KeyValueConfigurationParseDiagnostic> diagnostics,
        string line,
        int lineNumber)
    {
        var trimmed = line.Trim();
        var separatorIndex = trimmed.IndexOf('=', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            AddMalformedLineDiagnostic(diagnostics, lineNumber);
            return false;
        }

        var key = trimmed[..separatorIndex].Trim();
        if (key.Length == 0)
        {
            AddMalformedLineDiagnostic(diagnostics, lineNumber);
            return false;
        }

        var value = trimmed[(separatorIndex + 1)..].Trim();
        values[key] = value;
        return true;
    }

    /// <summary>
    ///     Determines whether the supplied line should be ignored because it is blank or a
    ///     comment.
    /// </summary>
    /// <param name="line">The raw configuration line.</param>
    /// <returns><see langword="true" /> when the line should be skipped.</returns>
    private static bool CanSkipLine(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Length == 0 || trimmed.StartsWith('#');
    }
}
