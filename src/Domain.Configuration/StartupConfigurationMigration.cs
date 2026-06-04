using Proxyfan.Domain.Configuration.Migration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Static helper that runs the configuration migration pipeline against the user's
///     persisted <c>config.kv</c> file at application startup. Locates the file under
///     the supplied configuration directory, applies the migration pipeline, and writes
///     the migrated values back (with a backup of the original). Legacy
///     <c>config.yaml</c> files are still loaded and renamed to <c>config.kv</c>.
/// </summary>
public static class StartupConfigurationMigration
{
    /// <summary>
    ///     The well-known configuration file name persisted in
    ///     <c>%LOCALAPPDATA%/Proxyfan/</c>.
    /// </summary>
    public const string ConfigurationFileName = "config.kv";

    /// <summary>
    ///     The previous configuration file name used before renaming to
    ///     <see cref="ConfigurationFileName" />.
    /// </summary>
    public const string LegacyConfigurationFileName = "config.yaml";

    /// <summary>
    ///     Locates the configuration file under <paramref name="configurationDirectory" />,
    ///     preferring <c>config.kv</c> and falling back to legacy <c>config.yaml</c>.
    ///     Applies the migration pipeline composed from <paramref name="migrators" />, and
    ///     returns the migration outcome. If the file does not exist, an empty result is returned
    ///     (no work performed).
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
        var configurationFilePath = ResolveConfigurationFilePath(configurationDirectory);
        var pipeline = new ConfigurationMigrationPipeline(migrators);
        var loader = new FileConfigurationLoader(configurationFilePath, pipeline, targetVersion);
        var result = loader.Load();
        TryRenameLegacyConfigurationFile(configurationDirectory, configurationFilePath);
        return result;
    }

    private static string ResolveConfigurationFilePath(string configurationDirectory)
    {
        var configurationFilePath = Path.Combine(configurationDirectory, ConfigurationFileName);
        if (File.Exists(configurationFilePath))
        {
            return configurationFilePath;
        }

        var legacyConfigurationFilePath = Path.Combine(configurationDirectory, LegacyConfigurationFileName);
        return File.Exists(legacyConfigurationFilePath) ? legacyConfigurationFilePath : configurationFilePath;
    }

    private static void TryRenameLegacyConfigurationFile(string configurationDirectory, string configurationFilePath)
    {
        var legacyConfigurationFilePath = Path.Combine(configurationDirectory, LegacyConfigurationFileName);
        if (!string.Equals(configurationFilePath, legacyConfigurationFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var currentConfigurationFilePath = Path.Combine(configurationDirectory, ConfigurationFileName);
        if (File.Exists(currentConfigurationFilePath) || !File.Exists(legacyConfigurationFilePath))
        {
            return;
        }

        File.Move(legacyConfigurationFilePath, currentConfigurationFilePath);
    }
}
