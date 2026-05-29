using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Writes a <see cref="ConfigurationSnapshot" /> back out to the same minimal
///     <c>key=value</c> text format consumed by <see cref="KeyValueConfigurationParser" />.
///     Keys are sorted alphabetically (case-insensitive) so the output is stable and
///     diff-friendly. Used by the migrating configuration store to persist migrated values.
/// </summary>
public static class KeyValueConfigurationWriter
{
    /// <summary>
    ///     Renders the supplied snapshot as <c>key=value</c> text with one entry per line and
    ///     a single trailing newline.
    /// </summary>
    /// <param name="snapshot">The snapshot to render.</param>
    /// <returns>The text representation.</returns>
    public static string Write(ConfigurationSnapshot snapshot)
    {
        var entries = new List<KeyValuePair<string, string>>(snapshot.Count);

        foreach (var entry in snapshot.Enumerate())
        {
            entries.Add(entry);
        }

        return WriteEntries(entries);
    }

    /// <summary>
    ///     Renders the supplied key-value pairs as <c>key=value</c> text with one entry per
    ///     line and a single trailing newline. Keys are emitted in case-insensitive
    ///     alphabetical order.
    /// </summary>
    /// <param name="values">The key-value pairs to render.</param>
    /// <returns>The text representation.</returns>
    public static string Write(IReadOnlyDictionary<string, string> values)
    {
        var entries = new List<KeyValuePair<string, string>>(values.Count);

        foreach (var entry in values)
        {
            entries.Add(entry);
        }

        return WriteEntries(entries);
    }

    private static string WriteEntries(List<KeyValuePair<string, string>> entries)
    {
        entries.Sort(static (left, right) => string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase));
        using var writer = new StringWriter();

        foreach (var entry in entries)
        {
            writer.Write(entry.Key);
            writer.Write('=');
            writer.WriteLine(entry.Value);
        }

        return writer.ToString();
    }
}
