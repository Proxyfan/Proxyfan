using System.Collections.Generic;
using System.Text.Json;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Parses the JSON manifest produced by the plugin update endpoint. The expected
///     schema is:
///     <code>
///     {
///       "plugins": [
///         { "id": "string", "latestVersion": "string", "downloadUrl": "string", "minApiVersion": "string" }
///       ]
///     }
///     </code>
///     Malformed input (invalid JSON, wrong shape, or entries missing any of
///     <c>id</c>, <c>latestVersion</c>, <c>downloadUrl</c>, or <c>minApiVersion</c>)
///     returns <see langword="null" /> or, for individual entries, skips the entry.
/// </summary>
public static class PluginUpdateManifestParser
{
    /// <summary>
    ///     Attempts to parse the supplied JSON manifest payload.
    /// </summary>
    /// <param name="payload">The raw JSON text.</param>
    /// <returns>The parsed manifest, or <see langword="null" /> when the payload is missing or malformed.</returns>
    public static PluginUpdateManifest? TryParse(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            return BuildManifest(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PluginUpdateEntry? BuildEntry(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var identifier = ReadString(element, "id");
        var latestVersion = ReadString(element, "latestVersion");
        var downloadUrl = ReadString(element, "downloadUrl");
        var minimumApiVersion = ReadString(element, "minApiVersion");
        if (string.IsNullOrWhiteSpace(identifier)
            || string.IsNullOrWhiteSpace(latestVersion)
            || string.IsNullOrWhiteSpace(downloadUrl)
            || string.IsNullOrWhiteSpace(minimumApiVersion))
        {
            return null;
        }

        var entry = new PluginUpdateEntry
        {
            Identifier = identifier,
            LatestVersion = latestVersion,
            DownloadUrl = downloadUrl,
            MinimumApiVersion = minimumApiVersion,
        };
        return entry;
    }

    private static PluginUpdateManifest? BuildManifest(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!root.TryGetProperty("plugins", out var pluginsElement))
        {
            return null;
        }

        if (pluginsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var entries = new List<PluginUpdateEntry>();
        foreach (var element in pluginsElement.EnumerateArray())
        {
            var entry = BuildEntry(element);
            if (entry is null)
            {
                continue;
            }

            entries.Add(entry);
        }

        var manifest = new PluginUpdateManifest
        {
            Plugins = entries,
        };
        return manifest;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }
}
