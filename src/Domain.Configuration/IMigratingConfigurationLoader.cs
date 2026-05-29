namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Loads a <see cref="ConfigurationSnapshot" /> from a configuration source while
///     applying any registered
///     <see cref="Migration.ConfigurationMigrationPipeline" /> transforms so that older
///     on-disk schema versions are upgraded transparently before the application consumes
///     the values.
/// </summary>
public interface IMigratingConfigurationLoader
{
    /// <summary>
    ///     Loads the configuration, runs the migration pipeline (if necessary), and returns
    ///     the migrated snapshot together with the migration record so callers can log or
    ///     surface the actions that were applied.
    /// </summary>
    /// <returns>The load result with the migrated snapshot.</returns>
    MigratingConfigurationLoadResult Load();
}
