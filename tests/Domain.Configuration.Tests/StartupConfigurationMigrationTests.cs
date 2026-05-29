using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Proxyfan.Domain.Configuration.Migration;

namespace Proxyfan.Domain.Configuration.Tests;

/// <summary>
///     Tests for <see cref="StartupConfigurationMigration" />.
/// </summary>
public sealed class StartupConfigurationMigrationTests
{
    /// <summary>
    ///     Verifies that running the migration when no <c>config.yaml</c> exists returns an
    ///     empty result that does not write any file.
    /// </summary>
    [Test]
    public async Task Run_MissingFile_ReturnsEmptyResultAndDoesNotCreateFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            var migrators = Array.Empty<IConfigurationMigrator>();
            var targetVersion = new ConfigurationVersion(1, 0);

            var result = StartupConfigurationMigration.Run(directory, migrators, targetVersion);

            await Assert.That(result.PipelineResult.IsMigrated).IsFalse();
            await Assert.That(File.Exists(Path.Combine(directory, "config.yaml"))).IsFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that running the migration on a file already at the target version is a
    ///     no-op (no migration actions, no backup file).
    /// </summary>
    [Test]
    public async Task Run_FileAlreadyAtTargetVersion_NoOp()
    {
        var directory = CreateTempDirectory();
        try
        {
            var configPath = Path.Combine(directory, "config.yaml");
            File.WriteAllText(configPath, "version=1.0\nproxy.port=8080\n");
            var migrators = Array.Empty<IConfigurationMigrator>();
            var targetVersion = new ConfigurationVersion(1, 0);

            var result = StartupConfigurationMigration.Run(directory, migrators, targetVersion);

            await Assert.That(result.PipelineResult.IsMigrated).IsFalse();
            await Assert.That(File.Exists(configPath + ".bak")).IsFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that an older-version configuration is migrated forward by a registered
    ///     migrator: the file is rewritten with the migrated values and a backup of the
    ///     original is created.
    /// </summary>
    [Test]
    public async Task Run_OlderVersion_AppliesMigratorAndWritesBackup()
    {
        var directory = CreateTempDirectory();
        try
        {
            var configPath = Path.Combine(directory, "config.yaml");
            File.WriteAllText(configPath, "version=1.0\nold.key=value\n");
            var migrator = new ConfigurationMigrator
            {
                From = new ConfigurationVersion(1, 0),
                To = new ConfigurationVersion(2, 0),
                Operations = new List<IConfigurationMigrationOperation>
                {
                    new ConfigurationRenameKeyOperation { OldKey = "old.key", NewKey = "new.key" },
                },
            };
            var targetVersion = new ConfigurationVersion(2, 0);

            var result = StartupConfigurationMigration.Run(directory, new[] { migrator }, targetVersion);

            await Assert.That(result.PipelineResult.IsMigrated).IsTrue();
            await Assert.That(result.PipelineResult.SourceVersion).IsEqualTo(new ConfigurationVersion(1, 0));
            await Assert.That(result.PipelineResult.TargetVersion).IsEqualTo(new ConfigurationVersion(2, 0));
            await Assert.That(File.Exists(configPath + ".bak")).IsTrue();

            var rewritten = File.ReadAllText(configPath);
            await Assert.That(rewritten).Contains("new.key=value");
            await Assert.That(rewritten).Contains("version=2.0");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-startupmigration-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }
}
