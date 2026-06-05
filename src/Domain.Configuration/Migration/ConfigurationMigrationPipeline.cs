using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     Chains a set of <see cref="IConfigurationMigrator" /> instances together so that a
///     configuration snapshot stored at any historical schema version can be transformed
///     into the running application's target schema version. Migrators are matched by their
///     <see cref="IConfigurationMigrator.From" /> version; the pipeline runs them in
///     ascending order until either the target version is reached or no further migrator
///     covers the current version.
/// </summary>
public sealed class ConfigurationMigrationPipeline
{
    private readonly Dictionary<ConfigurationVersion, IConfigurationMigrator> _migratorsByFrom;

    /// <summary>
    ///     Initializes a new <see cref="ConfigurationMigrationPipeline" /> with the supplied
    ///     migrators. Each migrator must have a unique <see cref="IConfigurationMigrator.From" />
    ///     version.
    /// </summary>
    /// <param name="migrators">The migrators that compose the pipeline.</param>
    /// <exception cref="ArgumentException">
    ///     Two migrators share the same <see cref="IConfigurationMigrator.From" /> version.
    /// </exception>
    public ConfigurationMigrationPipeline(IEnumerable<IConfigurationMigrator> migrators)
    {
        var byFrom = new Dictionary<ConfigurationVersion, IConfigurationMigrator>();
        foreach (var migrator in migrators)
        {
            if (byFrom.ContainsKey(migrator.From))
            {
                throw new ArgumentException(
                    $"Duplicate configuration migrator for source version {migrator.From}.",
                    nameof(migrators));
            }

            byFrom[migrator.From] = migrator;
        }

        _migratorsByFrom = byFrom;
    }

    /// <summary>
    ///     Migrates <paramref name="source" /> from its declared schema version (read from
    ///     the <see cref="ConfigurationMigrationConstants.VersionKey" /> key) to
    ///     <paramref name="targetVersion" /> by chaining matching migrators in ascending
    ///     version order.
    /// </summary>
    /// <param name="source">The configuration values to migrate.</param>
    /// <param name="targetVersion">The desired target schema version.</param>
    /// <returns>The migration result.</returns>
    /// <exception cref="InvalidOperationException">
    ///     The source configuration version is newer than the supported
    ///     <paramref name="targetVersion" /> (future-version incompatibility);
    ///     or
    ///     A migrator was needed to transition from the current version to the target but
    ///     none was registered, leaving the pipeline unable to make progress;
    ///     or a registered migrator's <see cref="IConfigurationMigrator.To" /> version does
    ///     not strictly exceed its <see cref="IConfigurationMigrator.From" /> version (no
    ///     forward progress); or a registered migrator's <see cref="IConfigurationMigrator.To" />
    ///     version exceeds <paramref name="targetVersion" /> (overshoot).
    /// </exception>
    public ConfigurationMigrationPipelineResult Migrate(
        IReadOnlyDictionary<string, string> source,
        ConfigurationVersion targetVersion)
    {
        var sourceVersion = ConfigurationMigrationPipelineHelpers.ReadVersion(source);
        if (sourceVersion == targetVersion)
        {
            var unchangedValues = ConfigurationMigrationPipelineHelpers.CopyValues(source);
            unchangedValues[ConfigurationMigrationConstants.VersionKey] = targetVersion.ToString();
            var noopResult = new ConfigurationMigrationPipelineResult
            {
                Actions = [],
                IsMigrated = false,
                SourceVersion = sourceVersion,
                TargetVersion = targetVersion,
                Values = unchangedValues,
            };
            return noopResult;
        }

        if (sourceVersion > targetVersion)
        {
            throw new InvalidOperationException(
                $"Configuration source version {sourceVersion} is newer than supported target version {targetVersion}.");
        }

        IReadOnlyDictionary<string, string> currentValues = ConfigurationMigrationPipelineHelpers.CopyValues(source);
        var aggregateActions = new List<ConfigurationMigrationAction>();
        var currentVersion = sourceVersion;
        while (currentVersion.HasLowerOrderThan(targetVersion))
        {
            if (!_migratorsByFrom.TryGetValue(currentVersion, out var migrator))
            {
                throw new InvalidOperationException(
                    $"No configuration migrator registered for source version {currentVersion}.");
            }

            ConfigurationMigrationPipelineHelpers.ValidateMigratorTransition(migrator, currentVersion, targetVersion);

            var stepResult = migrator.Apply(currentValues);
            currentValues = stepResult.Values;
            aggregateActions.AddRange(stepResult.Actions);
            currentVersion = migrator.To;
        }

        var result = new ConfigurationMigrationPipelineResult
        {
            Actions = aggregateActions,
            IsMigrated = true,
            SourceVersion = sourceVersion,
            TargetVersion = currentVersion,
            Values = currentValues,
        };
        return result;
    }
}
