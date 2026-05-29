using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     An <see cref="IConfigurationMigrator" /> implementation composed of a sequence of
///     <see cref="IConfigurationMigrationOperation" /> instances applied in order. The
///     final action recorded is always a <see cref="ConfigurationMigrationActionKind.VersionBumped" />
///     so the resulting snapshot carries the migrator's <see cref="To" /> version under the
///     well-known <see cref="ConfigurationMigrationConstants.VersionKey" /> key.
/// </summary>
public sealed class ConfigurationMigrator : IConfigurationMigrator
{
    /// <summary>
    ///     Gets the ordered list of operations applied by this migrator.
    /// </summary>
    public required IReadOnlyList<IConfigurationMigrationOperation> Operations { get; init; }

    /// <inheritdoc />
    public ConfigurationMigratorResult Apply(IReadOnlyDictionary<string, string> source)
    {
        var working = ConfigurationMigrationPipelineHelpers.CopyValues(source);
        var actions = new List<ConfigurationMigrationAction>();
        foreach (var operation in Operations)
        {
            operation.Apply(working, actions);
        }

        var toText = To.ToString();
        working[ConfigurationMigrationConstants.VersionKey] = toText;
        var versionAction = new ConfigurationMigrationAction
        {
            Key = ConfigurationMigrationConstants.VersionKey,
            Kind = ConfigurationMigrationActionKind.VersionBumped,
            Value = toText,
        };
        actions.Add(versionAction);

        var result = new ConfigurationMigratorResult
        {
            Actions = actions,
            Values = working,
        };
        return result;
    }

    /// <inheritdoc />
    public required ConfigurationVersion From { get; init; }

    /// <inheritdoc />
    public required ConfigurationVersion To { get; init; }
}
