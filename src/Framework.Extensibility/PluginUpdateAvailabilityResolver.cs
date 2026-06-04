using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Joins an <see cref="IPluginRegistry" /> snapshot against a fetched
///     <see cref="PluginUpdateManifest" /> and emits one
///     <see cref="PluginUpdateAvailability" /> entry per plugin where the manifest version
///     is strictly newer than the installed version.
/// </summary>
public static class PluginUpdateAvailabilityResolver
{
    /// <summary>
    ///     Computes the list of available updates.
    /// </summary>
    /// <param name="registry">The plugin registry.</param>
    /// <param name="manifest">The fetched manifest; pass <see langword="null" /> to clear.</param>
    /// <param name="hostApiVersion">The host API version used for compatibility flagging.</param>
    /// <returns>The list of updates with one row per upgradable plugin.</returns>
    public static IReadOnlyList<PluginUpdateAvailability> Resolve(
        IPluginRegistry registry,
        PluginUpdateManifest? manifest,
        string hostApiVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostApiVersion);
        if (manifest is null)
        {
            return [];
        }

        var entriesById = BuildEntriesById(manifest);
        var availabilities = new List<PluginUpdateAvailability>();
        foreach (var plugin in registry.Plugins)
        {
            var identifier = plugin.Metadata.Id;
            if (!entriesById.TryGetValue(identifier, out var entry))
            {
                continue;
            }

            if (!PluginVersionComparer.HasNewerCandidate(plugin.Metadata.Version, entry.LatestVersion))
            {
                continue;
            }

            var isCompatible = PluginApiVersionChecker.HasCompatibility(hostApiVersion, entry.MinimumApiVersion);
            var availability = new PluginUpdateAvailability
            {
                Identifier = identifier,
                Name = plugin.Metadata.Name,
                Author = plugin.Metadata.Author,
                CurrentVersion = plugin.Metadata.Version,
                LatestVersion = entry.LatestVersion,
                DownloadUrl = entry.DownloadUrl,
                IsCompatible = isCompatible,
            };
            availabilities.Add(availability);
        }

        return availabilities;
    }

    private static Dictionary<string, PluginUpdateEntry> BuildEntriesById(PluginUpdateManifest manifest)
    {
        var map = new Dictionary<string, PluginUpdateEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in manifest.Plugins)
        {
            if (string.IsNullOrWhiteSpace(entry.Identifier))
            {
                continue;
            }

            map[entry.Identifier] = entry;
        }

        return map;
    }
}
