using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginDirectoryScanner" />.
/// </summary>
[NotInParallel]
public sealed class PluginDirectoryScannerTests
{
    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-scan-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    ///     Verifies that a non-existent root directory yields an empty list.
    /// </summary>
    [Test]
    public async Task Scan_NonExistentRoot_ReturnsEmpty()
    {
        var scanner = new PluginDirectoryScanner();
        var nonExistent = Path.Combine(Path.GetTempPath(), "proxyfan-missing-" + Path.GetRandomFileName());

        var candidates = scanner.Scan(nonExistent);

        await Assert.That(candidates.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that an empty root directory yields an empty list.
    /// </summary>
    [Test]
    public async Task Scan_EmptyRoot_ReturnsEmpty()
    {
        var scanner = new PluginDirectoryScanner();
        var root = CreateTempDirectory();
        try
        {
            var candidates = scanner.Scan(root);

            await Assert.That(candidates.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that a mix of valid and invalid sub-directories is returned in alphabetical order.
    /// </summary>
    [Test]
    public async Task Scan_ValidAndInvalidDirectories_ReturnsBothInOrder()
    {
        var scanner = new PluginDirectoryScanner();
        var root = CreateTempDirectory();
        try
        {
            var alpha = Directory.CreateDirectory(Path.Combine(root, "alpha"));
            var beta = Directory.CreateDirectory(Path.Combine(root, "beta"));
            File.WriteAllText(Path.Combine(alpha.FullName, "plugin.manifest"), """
                id=alpha
                name=Alpha
                version=1
                author=A
                description=D
                apiVersion=1.0
                assembly=A.dll
                entryType=A.M
                """);
            // beta has no manifest -> invalid
            _ = beta;

            var candidates = scanner.Scan(root).ToArray();

            await Assert.That(candidates.Length).IsEqualTo(2);
            await Assert.That(candidates[0].IsValid).IsTrue();
            await Assert.That(candidates[0].Manifest!.Metadata.Id).IsEqualTo("alpha");
            await Assert.That(candidates[1].IsValid).IsFalse();
            await Assert.That(candidates[1].ErrorMessage).Contains("Missing manifest");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
