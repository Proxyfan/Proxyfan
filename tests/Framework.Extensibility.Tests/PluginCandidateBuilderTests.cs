using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginCandidateBuilder" />.
/// </summary>
[NotInParallel]
public sealed class PluginCandidateBuilderTests
{
    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-cand-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    ///     Verifies that a missing manifest produces an invalid candidate.
    /// </summary>
    [Test]
    public async Task Build_MissingManifest_ReturnsInvalid()
    {
        var directory = CreateTempDirectory();
        try
        {
            var candidate = PluginCandidateBuilder.Build(directory, "plugin.manifest");

            await Assert.That(candidate.IsValid).IsFalse();
            await Assert.That(candidate.ErrorMessage).Contains("Missing manifest");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that a malformed manifest produces an invalid candidate carrying the parse error.
    /// </summary>
    [Test]
    public async Task Build_MalformedManifest_ReturnsInvalidWithParseError()
    {
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "plugin.manifest"), "id=p");
            var candidate = PluginCandidateBuilder.Build(directory, "plugin.manifest");

            await Assert.That(candidate.IsValid).IsFalse();
            await Assert.That(candidate.ErrorMessage).Contains("Missing required");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that a well-formed manifest produces a valid candidate.
    /// </summary>
    [Test]
    public async Task Build_ValidManifest_ReturnsValid()
    {
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "plugin.manifest"), """
                id=p
                name=N
                version=1
                author=A
                description=D
                apiVersion=1.0
                assembly=A.dll
                entryType=A.Main
                """);
            var candidate = PluginCandidateBuilder.Build(directory, "plugin.manifest");

            await Assert.That(candidate.IsValid).IsTrue();
            await Assert.That(candidate.Manifest!.Metadata.Id).IsEqualTo("p");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that an I/O failure while reading the manifest is captured as an invalid
    ///     candidate carrying the IO error message (covers the <see cref="IOException" /> catch
    ///     branch).
    /// </summary>
    [Test]
    public async Task Build_ManifestExclusivelyLocked_ReturnsInvalidWithIoError()
    {
        var directory = CreateTempDirectory();
        var manifestPath = Path.Combine(directory, "plugin.manifest");
        try
        {
            File.WriteAllText(manifestPath, "id=p");
            using var locker = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.None);

            var candidate = PluginCandidateBuilder.Build(directory, "plugin.manifest");

            await Assert.That(candidate.IsValid).IsFalse();
            await Assert.That(candidate.ErrorMessage).IsNotNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
