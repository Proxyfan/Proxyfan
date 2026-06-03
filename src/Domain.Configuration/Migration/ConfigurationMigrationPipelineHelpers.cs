using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     Static helpers used by <see cref="ConfigurationMigrationPipeline" /> to inspect and
///     duplicate configuration value snapshots. Extracted into a dedicated static class so
///     the pipeline complies with the static-methods-only-on-static-classes analyzer rule.
/// </summary>
public static class ConfigurationMigrationPipelineHelpers
{
    /// <summary>
    ///     Returns a mutable, case-insensitive duplicate of <paramref name="source" /> safe to
    ///     mutate without affecting the caller.
    /// </summary>
    /// <param name="source">The values to copy.</param>
    /// <returns>A new mutable dictionary with the same contents.</returns>
    public static Dictionary<string, string> CopyValues(IReadOnlyDictionary<string, string> source)
    {
        var copy = new Dictionary<string, string>(source.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    /// <summary>
    ///     Reads the <see cref="ConfigurationMigrationConstants.VersionKey" /> value from
    ///     <paramref name="source" /> and parses it as a <see cref="ConfigurationVersion" />.
    ///     When the key is missing or blank the version is assumed to be <c>1.0</c>.
    /// </summary>
    /// <param name="source">The configuration values to inspect.</param>
    /// <returns>The detected configuration version.</returns>
    public static ConfigurationVersion ReadVersion(IReadOnlyDictionary<string, string> source)
    {
        if (!source.TryGetValue(ConfigurationMigrationConstants.VersionKey, out var text)
            || string.IsNullOrWhiteSpace(text))
        {
            return new ConfigurationVersion(1, 0);
        }

        return ConfigurationVersion.Parse(text);
    }

    /// <summary>
    ///     Validates that <paramref name="migrator" /> makes strict forward progress and does
    ///     not overshoot <paramref name="targetVersion" />.
    /// </summary>
    /// <param name="migrator">The migrator about to be applied.</param>
    /// <param name="currentVersion">The version the pipeline is currently at.</param>
    /// <param name="targetVersion">The version the pipeline must reach.</param>
    /// <exception cref="InvalidOperationException">
    ///     The migrator's <see cref="IConfigurationMigrator.To" /> version does not strictly
    ///     exceed <paramref name="currentVersion" /> (no forward progress), or it exceeds
    ///     <paramref name="targetVersion" /> (overshoot).
    /// </exception>
    public static void ValidateMigratorTransition(
        IConfigurationMigrator migrator,
        ConfigurationVersion currentVersion,
        ConfigurationVersion targetVersion)
    {
        if (!currentVersion.HasLowerOrderThan(migrator.To))
        {
            throw new InvalidOperationException(
                $"Configuration migrator from {migrator.From} to {migrator.To} does not advance the version beyond {currentVersion}.");
        }

        if (migrator.To > targetVersion)
        {
            throw new InvalidOperationException(
                $"Configuration migrator from {migrator.From} to {migrator.To} overshoots the target version {targetVersion}.");
        }
    }
}
