using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Merges multiple <see cref="ConfigurationSnapshot" /> instances into a single snapshot
///     using a fixed precedence order (later sources override earlier ones).
/// </summary>
public static class ConfigurationMerger
{
    /// <summary>
    ///     Merges all supplied snapshots in order, with later snapshots overriding values
    ///     from earlier snapshots.
    /// </summary>
    /// <param name="snapshots">The snapshots to merge (lowest to highest priority).</param>
    /// <returns>The combined snapshot.</returns>
    public static ConfigurationSnapshot Merge(IReadOnlyList<ConfigurationSnapshot> snapshots)
    {
        var combined = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < snapshots.Count; index++)
        {
            var snapshot = snapshots[index];
            foreach (var pair in snapshot.Enumerate())
            {
                combined[pair.Key] = pair.Value;
            }
        }

        var result = new ConfigurationSnapshot(combined);
        return result;
    }
}
