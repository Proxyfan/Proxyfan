using Proxyfan.Domain.Configuration.Migration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     File-backed <see cref="IMigratingConfigurationLoader" /> that reads a
///     <c>key=value</c> text file, applies the supplied
///     <see cref="ConfigurationMigrationPipeline" />, and rewrites the file with the
///     migrated values. The original file is backed up to <c>&lt;path&gt;.bak</c> before
///     overwriting so that a user can recover from an unexpected migration.
///     If the file contains malformed lines the load is rejected: no migration is applied
///     and the file is not rewritten, preventing a bad parse from overwriting good data.
/// </summary>
public sealed class FileConfigurationLoader : IMigratingConfigurationLoader
{
    /// <summary>
    ///     The file extension appended to the configuration path when creating a backup
    ///     prior to writing migrated values.
    /// </summary>
    public const string BackupExtension = ".bak";
    private readonly string _filePath;
    private readonly ConfigurationMigrationPipeline _pipeline;
    private readonly ConfigurationVersion _targetVersion;

    /// <summary>
    ///     Initializes a new <see cref="FileConfigurationLoader" /> backed by the supplied
    ///     file path and migration pipeline.
    /// </summary>
    /// <param name="filePath">The absolute path to the configuration file.</param>
    /// <param name="pipeline">The migration pipeline used to upgrade older schema versions.</param>
    /// <param name="targetVersion">The schema version expected by the running application.</param>
    public FileConfigurationLoader(
        string filePath,
        ConfigurationMigrationPipeline pipeline,
        ConfigurationVersion targetVersion)
    {
        _filePath = filePath;
        _pipeline = pipeline;
        _targetVersion = targetVersion;
    }

    /// <inheritdoc />
    public MigratingConfigurationLoadResult Load()
    {
        if (!File.Exists(_filePath))
        {
            return BuildEmptyResult();
        }

        string text;
        try
        {
            text = File.ReadAllText(_filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return BuildIoReadFailureResult(ex);
        }

        var parseResult = KeyValueConfigurationParser.Parse(text);

        if (!parseResult.IsSuccess)
        {
            return BuildMalformedResult(parseResult);
        }

        var snapshot = parseResult.Snapshot;
        var sourceValues = new Dictionary<string, string>();

        foreach (var pair in snapshot.Enumerate())
        {
            sourceValues[pair.Key] = pair.Value;
        }

        var pipelineResult = _pipeline.Migrate(sourceValues, _targetVersion);

        if (!pipelineResult.IsMigrated)
        {
            var unchanged = new ConfigurationSnapshot(pipelineResult.Values);
            return new MigratingConfigurationLoadResult
            {
                BackupPath = null,
                IoFailure = null,
                MalformedLines = [],
                PipelineResult = pipelineResult,
                Snapshot = unchanged,
            };
        }

        return PersistMigratedConfiguration(snapshot, pipelineResult);
    }

    private MigratingConfigurationLoadResult BuildEmptyResult()
    {
        var emptyValues = new Dictionary<string, string>
        {
            [ConfigurationMigrationConstants.VersionKey] = _targetVersion.ToString(),
        };
        var pipelineResult = new ConfigurationMigrationPipelineResult
        {
            Actions = [],
            IsMigrated = false,
            SourceVersion = _targetVersion,
            TargetVersion = _targetVersion,
            Values = emptyValues,
        };
        var snapshot = new ConfigurationSnapshot(emptyValues);
        return new MigratingConfigurationLoadResult
        {
            BackupPath = null,
            IoFailure = null,
            MalformedLines = null,
            PipelineResult = pipelineResult,
            Snapshot = snapshot,
        };
    }

    /// <summary>
    ///     Builds a <see cref="MigratingConfigurationLoadResult" /> for the case where the
    ///     backup or write of the migrated configuration file failed with an IO exception.
    ///     The pre-migration snapshot is returned so the application can still run, and
    ///     <see cref="MigratingConfigurationLoadResult.IoFailure" /> carries the exception
    ///     for callers to log.
    /// </summary>
    /// <param name="ex">The IO exception that was caught.</param>
    /// <param name="preMigrationSnapshot">The snapshot loaded from the file before migration.</param>
    /// <param name="pipelineResult">The pipeline result that describes the attempted migration.</param>
    /// <returns>The failure result.</returns>
    private MigratingConfigurationLoadResult BuildIoPersistFailureResult(
        Exception ex,
        ConfigurationSnapshot preMigrationSnapshot,
        ConfigurationMigrationPipelineResult pipelineResult)
    {
        var notMigrated = new ConfigurationMigrationPipelineResult
        {
            Actions = pipelineResult.Actions,
            IsMigrated = false,
            SourceVersion = pipelineResult.SourceVersion,
            TargetVersion = pipelineResult.TargetVersion,
            Values = pipelineResult.Values,
        };
        return new MigratingConfigurationLoadResult
        {
            BackupPath = null,
            IoFailure = ex,
            MalformedLines = [],
            PipelineResult = notMigrated,
            Snapshot = preMigrationSnapshot,
        };
    }

    /// <summary>
    ///     Builds a <see cref="MigratingConfigurationLoadResult" /> for the case where the
    ///     configuration file could not be read due to an IO exception. A default empty
    ///     snapshot is returned so the application can start with default values, and
    ///     <see cref="MigratingConfigurationLoadResult.IoFailure" /> carries the exception
    ///     for callers to log.
    /// </summary>
    /// <param name="ex">The IO exception that was caught.</param>
    /// <returns>The failure result.</returns>
    private MigratingConfigurationLoadResult BuildIoReadFailureResult(Exception ex)
    {
        var emptyValues = new Dictionary<string, string>
        {
            [ConfigurationMigrationConstants.VersionKey] = _targetVersion.ToString(),
        };
        var pipelineResult = new ConfigurationMigrationPipelineResult
        {
            Actions = [],
            IsMigrated = false,
            SourceVersion = _targetVersion,
            TargetVersion = _targetVersion,
            Values = emptyValues,
        };
        var snapshot = new ConfigurationSnapshot(emptyValues);
        return new MigratingConfigurationLoadResult
        {
            BackupPath = null,
            IoFailure = ex,
            MalformedLines = null,
            PipelineResult = pipelineResult,
            Snapshot = snapshot,
        };
    }

    /// <summary>
    ///     Builds a <see cref="MigratingConfigurationLoadResult" /> for the case where the
    ///     configuration file contained malformed lines. Migration is not applied and the
    ///     file is not rewritten so that no good data can be lost due to a bad parse.
    /// </summary>
    /// <param name="parseResult">The failed parse result carrying the malformed lines.</param>
    /// <returns>The rejection result.</returns>
    private MigratingConfigurationLoadResult BuildMalformedResult(KeyValueConfigurationParseResult parseResult)
    {
        var rejectedValues = new Dictionary<string, string>();
        var pipelineResult = new ConfigurationMigrationPipelineResult
        {
            Actions = [],
            IsMigrated = false,
            SourceVersion = _targetVersion,
            TargetVersion = _targetVersion,
            Values = rejectedValues,
        };
        return new MigratingConfigurationLoadResult
        {
            BackupPath = null,
            IoFailure = null,
            MalformedLines = parseResult.MalformedLines,
            PipelineResult = pipelineResult,
            Snapshot = parseResult.Snapshot,
        };
    }

    /// <summary>
    ///     Attempts to back up the original file and write the migrated content to disk.
    ///     If either file-system operation fails with an IO exception the pre-migration
    ///     snapshot is returned with
    ///     <see cref="MigratingConfigurationLoadResult.IoFailure" /> set so the caller
    ///     can log the problem without crashing startup.
    /// </summary>
    /// <param name="preMigrationSnapshot">The snapshot loaded from the file before migration.</param>
    /// <param name="pipelineResult">The completed migration pipeline result.</param>
    /// <returns>The persist result, successful or failed.</returns>
    private MigratingConfigurationLoadResult PersistMigratedConfiguration(
        ConfigurationSnapshot preMigrationSnapshot,
        ConfigurationMigrationPipelineResult pipelineResult)
    {
        var backupPath = _filePath + BackupExtension;
        try
        {
            File.Copy(_filePath, backupPath, overwrite: true);
            var migratedText = KeyValueConfigurationWriter.Write(pipelineResult.Values);
            File.WriteAllText(_filePath, migratedText);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return BuildIoPersistFailureResult(ex, preMigrationSnapshot, pipelineResult);
        }

        var migrated = new ConfigurationSnapshot(pipelineResult.Values);
        return new MigratingConfigurationLoadResult
        {
            BackupPath = backupPath,
            IoFailure = null,
            MalformedLines = [],
            PipelineResult = pipelineResult,
            Snapshot = migrated,
        };
    }
}
