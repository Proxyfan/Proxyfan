using Proxyfan.Domain.Configuration.Migration;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Static helper that runs the configuration migration pipeline against the user's
///     persisted <c>config.yaml</c> file at application startup. Locates the file under
///     the supplied configuration directory, applies the migration pipeline, and writes
///     the migrated values back (with a backup of the original).
/// </summary>
public static class StartupConfigurationMigration
{
    /// <summary>
    ///     The well-known configuration file name persisted in
    ///     <c>%LOCALAPPDATA%/Proxyfan/</c>.
    /// </summary>
    public const string ConfigurationFileName = "config.yaml";

    /// <summary>
    ///     Locates the <c>config.yaml</c> file under <paramref name="configurationDirectory" />,
    ///     applies the migration pipeline composed from <paramref name="migrators" />, and
    ///     returns the migration outcome. If the file does not exist, an empty result is
    ///     returned (no work performed).
    /// </summary>
    /// <param name="configurationDirectory">
    ///     The directory that holds the user's configuration file (typically
    ///     <c>%LOCALAPPDATA%/Proxyfan/</c>).
    /// </param>
    /// <param name="migrators">
    ///     The migrators that compose the migration pipeline. May be empty; an empty pipeline
    ///     performs no transformations and returns the existing snapshot unchanged.
    /// </param>
    /// <param name="targetVersion">
    ///     The schema version expected by the running application.
    /// </param>
    /// <returns>The result of the migration attempt.</returns>
    public static MigratingConfigurationLoadResult Run(
        string configurationDirectory,
        IEnumerable<IConfigurationMigrator> migrators,
        ConfigurationVersion targetVersion)
    {
        var configurationFilePath = Path.Combine(configurationDirectory, ConfigurationFileName);
        var pipeline = new ConfigurationMigrationPipeline(migrators);
        var loader = new FileConfigurationLoader(configurationFilePath, pipeline, targetVersion);
        return loader.Load();
    }
}
