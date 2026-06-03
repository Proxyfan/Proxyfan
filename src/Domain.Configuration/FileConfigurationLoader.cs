using Proxyfan.Domain.Configuration.Migration;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     File-backed <see cref="IMigratingConfigurationLoader" /> that reads a
///     <c>key=value</c> text file, applies the supplied
///     <see cref="ConfigurationMigrationPipeline" />, and rewrites the file with the
///     migrated values. The original file is backed up to <c>&lt;path&gt;.bak</c> before
///     overwriting so that a user can recover from an unexpected migration.
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

        var text = File.ReadAllText(_filePath);
        var snapshot = ParseOrRejectMalformed(text);
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
                PipelineResult = pipelineResult,
                Snapshot = unchanged,
            };
        }

        var backupPath = _filePath + BackupExtension;
        File.Copy(_filePath, backupPath, overwrite: true);
        var migratedText = KeyValueConfigurationWriter.Write(pipelineResult.Values);
        File.WriteAllText(_filePath, migratedText);

        var migrated = new ConfigurationSnapshot(pipelineResult.Values);
        return new MigratingConfigurationLoadResult
        {
            BackupPath = backupPath,
            PipelineResult = pipelineResult,
            Snapshot = migrated,
        };
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
            PipelineResult = pipelineResult,
            Snapshot = snapshot,
        };
    }

    private string BuildMalformedConfigurationMessage(IReadOnlyList<string> malformedLines)
    {
        var message = "Malformed configuration lines were found:";
        foreach (var line in malformedLines)
        {
            message = string.Concat(message, " ", line);
        }

        return message;
    }

    private ConfigurationSnapshot ParseOrRejectMalformed(string text)
    {
        var parseResult = KeyValueConfigurationParser.ParseWithDiagnostics(text);
        if (parseResult.IsMalformedLinesPresent)
        {
            var message = BuildMalformedConfigurationMessage(parseResult.MalformedLines);
            throw new InvalidDataException(message);
        }

        return parseResult.Snapshot;
    }
}
