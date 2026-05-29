using System;
using System.IO;
using System.Threading.Tasks;
using Proxyfan.Client;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="AppStartupConfigurationMigrationRunner" />.
/// </summary>
public sealed class AppStartupConfigurationMigrationRunnerTests
{
    /// <summary>
    ///     Verifies that running against a directory that does not exist creates the
    ///     directory, returns an empty result, and does not throw.
    /// </summary>
    [Test]
    public async Task Run_MissingDirectory_CreatesDirectoryAndReturnsEmptyResult()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-app-migrator-" + Path.GetRandomFileName());
        try
        {
            var result = AppStartupConfigurationMigrationRunner.Run(directory);

            await Assert.That(Directory.Exists(directory)).IsTrue();
            await Assert.That(result.PipelineResult.IsMigrated).IsFalse();
            await Assert.That(result.Snapshot).IsNotNull();
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    ///     Verifies that an empty existing directory returns an empty result and does not
    ///     create any config file.
    /// </summary>
    [Test]
    public async Task Run_EmptyDirectory_ReturnsEmptyResult()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-app-migrator-" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            var result = AppStartupConfigurationMigrationRunner.Run(directory);

            await Assert.That(result.PipelineResult.IsMigrated).IsFalse();
            await Assert.That(Directory.GetFiles(directory).Length).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="AppStartupConfigurationMigrationRunner.CurrentConfigurationVersion" />
    ///     exposes the application's current configuration schema version.
    /// </summary>
    [Test]
    public async Task CurrentConfigurationVersion_Always_HasMajorOne()
    {
        var version = AppStartupConfigurationMigrationRunner.CurrentConfigurationVersion;

        await Assert.That(version.Major).IsEqualTo(1);
    }
}
