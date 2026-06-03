using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Proxyfan.Domain.Configuration;
using Proxyfan.Domain.Configuration.Migration;

namespace Proxyfan.Domain.Configuration.Tests;

/// <summary>
///     Tests for <see cref="FileConfigurationLoader" />.
/// </summary>
public sealed class FileConfigurationLoaderTests
{
    /// <summary>
    ///     A missing file yields an empty snapshot containing only the target version.
    /// </summary>
    [Test]
    public async Task Load_MissingFile_ReturnsSnapshotWithTargetVersion()
    {
        var path = CreateTempPath();
        var loader = new FileConfigurationLoader(path, BuildEmptyPipeline(), new ConfigurationVersion(1, 0));

        var result = loader.Load();

        await Assert.That(result.BackupPath).IsNull();
        await Assert.That(result.PipelineResult.IsMigrated).IsFalse();
        await Assert.That(result.Snapshot.Get(ConfigurationMigrationConstants.VersionKey, string.Empty)).IsEqualTo("1.0");
    }

    /// <summary>
    ///     A file already at the target version is returned unchanged; no backup is created.
    /// </summary>
    [Test]
    public async Task Load_AtTargetVersion_NoMigrationNoBackup()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText(path, "version=1.0\nproxy.port=8080\n");
            var loader = new FileConfigurationLoader(path, BuildEmptyPipeline(), new ConfigurationVersion(1, 0));

            var result = loader.Load();

            await Assert.That(result.BackupPath).IsNull();
            await Assert.That(result.PipelineResult.IsMigrated).IsFalse();
            await Assert.That(result.Snapshot.Get("proxy.port", string.Empty)).IsEqualTo("8080");
            await Assert.That(File.Exists(path + FileConfigurationLoader.BackupExtension)).IsFalse();
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + FileConfigurationLoader.BackupExtension);
        }
    }

    /// <summary>
    ///     A file below the target version is migrated, the migrated values are written, and
    ///     a backup of the original is created beside the file.
    /// </summary>
    [Test]
    public async Task Load_BelowTargetVersion_AppliesMigrationAndWritesBackup()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText(path, "version=1.0\nold.key=value\n");
            var operation = new ConfigurationRenameKeyOperation
            {
                NewKey = "new.key",
                OldKey = "old.key",
            };
            var migrator = new ConfigurationMigrator
            {
                From = new ConfigurationVersion(1, 0),
                Operations = [operation],
                To = new ConfigurationVersion(1, 1),
            };
            var pipeline = new ConfigurationMigrationPipeline([migrator]);
            var loader = new FileConfigurationLoader(path, pipeline, new ConfigurationVersion(1, 1));

            var result = loader.Load();

            await Assert.That(result.PipelineResult.IsMigrated).IsTrue();
            await Assert.That(result.Snapshot.Get("new.key", string.Empty)).IsEqualTo("value");
            await Assert.That(result.Snapshot.HasKey("old.key")).IsFalse();
            await Assert.That(result.BackupPath).IsEqualTo(path + FileConfigurationLoader.BackupExtension);
            await Assert.That(File.Exists(result.BackupPath!)).IsTrue();
            await Assert.That(File.ReadAllText(result.BackupPath!).Contains("old.key=value")).IsTrue();
            await Assert.That(File.ReadAllText(path).Contains("new.key=value")).IsTrue();
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + FileConfigurationLoader.BackupExtension);
        }
    }

    /// <summary>
    ///     An existing backup is overwritten on subsequent migrations.
    /// </summary>
    [Test]
    public async Task Load_BackupAlreadyExists_OverwritesBackup()
    {
        var path = CreateTempPath();
        var backupPath = path + FileConfigurationLoader.BackupExtension;

        try
        {
            File.WriteAllText(path, "version=1.0\nsetting=current\n");
            File.WriteAllText(backupPath, "stale backup contents");
            var migrator = new ConfigurationMigrator
            {
                From = new ConfigurationVersion(1, 0),
                Operations = [],
                To = new ConfigurationVersion(1, 1),
            };
            var pipeline = new ConfigurationMigrationPipeline([migrator]);
            var loader = new FileConfigurationLoader(path, pipeline, new ConfigurationVersion(1, 1));

            loader.Load();

            await Assert.That(File.ReadAllText(backupPath).Contains("setting=current")).IsTrue();
            await Assert.That(File.ReadAllText(backupPath).Contains("stale backup")).IsFalse();
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(backupPath);
        }
    }

    /// <summary>
    ///     Malformed configuration text is rejected, reported via diagnostics, and not rewritten.
    /// </summary>
    [Test]
    public async Task Load_MalformedConfiguration_RejectsWithoutRewriteOrBackup()
    {
        var path = CreateTempPath();
        var backupPath = path + FileConfigurationLoader.BackupExtension;

        try
        {
            const string malformedText = "version=1.0\nmalformed line\nold.key=value\n";
            File.WriteAllText(path, malformedText);
            var operation = new ConfigurationRenameKeyOperation
            {
                NewKey = "new.key",
                OldKey = "old.key",
            };
            var migrator = new ConfigurationMigrator
            {
                From = new ConfigurationVersion(1, 0),
                Operations = [operation],
                To = new ConfigurationVersion(1, 1),
            };
            var pipeline = new ConfigurationMigrationPipeline([migrator]);
            var loader = new FileConfigurationLoader(path, pipeline, new ConfigurationVersion(1, 1));

            var result = loader.Load();
            var fileAfterLoad = File.ReadAllText(path);

            await Assert.That(result.PipelineResult.IsMigrated).IsFalse();
            await Assert.That(result.BackupPath).IsNull();
            await Assert.That(result.ParseDiagnostics.Count).IsEqualTo(1);
            await Assert.That(result.ParseDiagnostics[0].LineNumber).IsEqualTo(2);
            await Assert.That(fileAfterLoad).IsEqualTo(malformedText);
            await Assert.That(File.Exists(backupPath)).IsFalse();
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(backupPath);
        }
    }

    private static ConfigurationMigrationPipeline BuildEmptyPipeline()
    {
        return new ConfigurationMigrationPipeline(new List<IConfigurationMigrator>());
    }

    private static string CreateTempPath()
    {
        return Path.Combine(Path.GetTempPath(), $"proxyfan-config-{Guid.NewGuid():N}.cfg");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
