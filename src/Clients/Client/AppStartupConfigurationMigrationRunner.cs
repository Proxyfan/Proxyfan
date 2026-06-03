using Proxyfan.Domain.Configuration;
using Proxyfan.Domain.Configuration.Migration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Proxyfan.Client;

/// <summary>
///     Static helper invoked from <see cref="App" /> during construction to apply the
///     persisted configuration migration pipeline against the user's
///     <c>%LOCALAPPDATA%/Proxyfan/config.yaml</c> before the host builder reads it. Lives
///     in its own type so that the analyzer's static-in-non-static-class rule (ATXCS011)
///     is satisfied and so the helper can be unit-tested in isolation.
/// </summary>
public static class AppStartupConfigurationMigrationRunner
{
    /// <summary>
    ///     The configuration schema version expected by the current build of the
    ///     application. Bumped whenever a new <see cref="IConfigurationMigrator" /> is
    ///     introduced.
    /// </summary>
    public static ConfigurationVersion CurrentConfigurationVersion
    {
        get
        {
            var version = new ConfigurationVersion(1, 0);
            return version;
        }
    }

    /// <summary>
    ///     Loads and (when required) migrates the configuration file in
    ///     <paramref name="userConfigurationDirectory" />. Any failure is swallowed and an
    ///     empty result is returned so the application can still start with default
    ///     configuration values.
    /// </summary>
    /// <param name="userConfigurationDirectory">
    ///     The directory that holds the user's configuration file (typically
    ///     <c>%LOCALAPPDATA%/Proxyfan/</c>).
    /// </param>
    /// <returns>The result of the migration attempt.</returns>
    public static MigratingConfigurationLoadResult Run(string userConfigurationDirectory)
    {
        try
        {
            Directory.CreateDirectory(userConfigurationDirectory);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Skipping configuration migration; cannot create config directory: {ex}");
            return BuildEmpty();
        }

        try
        {
            var migrators = Array.Empty<IConfigurationMigrator>();
            return StartupConfigurationMigration.Run(
                userConfigurationDirectory,
                migrators,
                CurrentConfigurationVersion);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Configuration migration failed at startup: {ex}");
            return BuildEmpty();
        }
    }

    private static MigratingConfigurationLoadResult BuildEmpty()
    {
        var emptyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pipelineResult = new ConfigurationMigrationPipelineResult
        {
            Actions = [],
            IsMigrated = false,
            SourceVersion = CurrentConfigurationVersion,
            TargetVersion = CurrentConfigurationVersion,
            Values = emptyValues,
        };
        var snapshot = new ConfigurationSnapshot(emptyValues);
        var result = new MigratingConfigurationLoadResult
        {
            BackupPath = null,
            MalformedLineNumbers = [],
            PipelineResult = pipelineResult,
            Snapshot = snapshot,
        };
        return result;
    }
}
