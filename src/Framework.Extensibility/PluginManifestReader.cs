using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Parses a minimal <c>key=value</c> plugin manifest into a
///     <see cref="PluginManifest" />. Required keys: <c>id</c>, <c>name</c>,
///     <c>version</c>, <c>author</c>, <c>description</c>, <c>apiVersion</c>,
///     <c>assembly</c>, <c>entryType</c>. Lines beginning with <c>#</c> are treated as
///     comments and ignored. Whitespace around keys and values is trimmed.
/// </summary>
public static class PluginManifestReader
{
    private const string ApiVersionKey = "apiVersion";
    private const string AssemblyKey = "assembly";
    private const string AuthorKey = "author";
    private const string DescriptionKey = "description";
    private const string EntryTypeKey = "entryType";
    private const string IdKey = "id";
    private const string NameKey = "name";
    private const string VersionKey = "version";

    /// <summary>
    ///     Parses the supplied manifest text into a <see cref="PluginManifestParseResult" />.
    /// </summary>
    /// <param name="text">The full text of the <c>plugin.manifest</c> file.</param>
    /// <returns>The parse result.</returns>
    public static PluginManifestParseResult Parse(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            if (key.Length > 0)
            {
                values[key] = value;
            }
        }

        var missingKey = FindMissingRequiredKey(values);
        if (missingKey is not null)
        {
            return PluginManifestParseResults.Failure($"Missing required manifest key '{missingKey}'.");
        }

        var metadata = new PluginMetadata(
            values[IdKey],
            values[NameKey],
            values[VersionKey],
            values[AuthorKey],
            values[DescriptionKey],
            values[ApiVersionKey]);
        var manifest = new PluginManifest(metadata, values[AssemblyKey], values[EntryTypeKey]);
        return PluginManifestParseResults.Success(manifest);
    }

    private static string? FindMissingRequiredKey(IReadOnlyDictionary<string, string> values)
    {
        if (HasMissingValue(values, IdKey))
        {
            return IdKey;
        }

        if (HasMissingValue(values, NameKey))
        {
            return NameKey;
        }

        if (HasMissingValue(values, VersionKey))
        {
            return VersionKey;
        }

        if (HasMissingValue(values, AuthorKey))
        {
            return AuthorKey;
        }

        if (HasMissingValue(values, DescriptionKey))
        {
            return DescriptionKey;
        }

        if (HasMissingValue(values, ApiVersionKey))
        {
            return ApiVersionKey;
        }

        if (HasMissingValue(values, AssemblyKey))
        {
            return AssemblyKey;
        }

        if (HasMissingValue(values, EntryTypeKey))
        {
            return EntryTypeKey;
        }

        return null;
    }

    private static bool HasMissingValue(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return false;
    }
}
