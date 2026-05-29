using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     A single transformer that brings a configuration snapshot from <see cref="From" /> to
///     <see cref="To" />. Each migrator is responsible for exactly one schema-version
///     transition; a multi-version upgrade chains several migrators together via
///     <see cref="ConfigurationMigrationPipeline" />.
/// </summary>
public interface IConfigurationMigrator
{
    /// <summary>
    ///     Gets the source schema version this migrator transforms from.
    /// </summary>
    ConfigurationVersion From { get; }

    /// <summary>
    ///     Gets the destination schema version this migrator transforms to.
    /// </summary>
    ConfigurationVersion To { get; }

    /// <summary>
    ///     Applies the migration to the supplied configuration values and returns the
    ///     transformed values together with a description of every action that was applied.
    ///     The implementation MUST NOT mutate the supplied dictionary.
    /// </summary>
    /// <param name="source">The current configuration values keyed by configuration name.</param>
    /// <returns>The migration result containing the transformed values and applied actions.</returns>
    ConfigurationMigratorResult Apply(IReadOnlyDictionary<string, string> source);
}
