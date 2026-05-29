using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="FilePluginEnabledStateStore" />.
/// </summary>
[NotInParallel]
public sealed class FilePluginEnabledStateStoreTests
{
    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-en-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    ///     Verifies that a missing file yields an empty set.
    /// </summary>
    [Test]
    public async Task GetDisabledIdentifiers_MissingFile_ReturnsEmpty()
    {
        var directory = CreateTempDirectory();
        try
        {
            var store = new FilePluginEnabledStateStore(Path.Combine(directory, "disabled.txt"));

            var disabled = store.GetDisabledIdentifiers();

            await Assert.That(disabled.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that SetEnabled(false) adds the identifier and persists it to disk.
    /// </summary>
    [Test]
    public async Task SetEnabled_FalseTwice_PersistsBothToDisk()
    {
        var directory = CreateTempDirectory();
        var filePath = Path.Combine(directory, "disabled.txt");
        try
        {
            var store = new FilePluginEnabledStateStore(filePath);

            store.SetEnabled("alpha", false);
            store.SetEnabled("beta", false);

            var disabled = store.GetDisabledIdentifiers();
            await Assert.That(disabled.Count).IsEqualTo(2);
            await Assert.That(disabled.Contains("alpha")).IsTrue();
            await Assert.That(disabled.Contains("beta")).IsTrue();

            var fresh = new FilePluginEnabledStateStore(filePath);
            await Assert.That(fresh.GetDisabledIdentifiers().Count).IsEqualTo(2);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that SetEnabled(true) removes a previously-disabled identifier.
    /// </summary>
    [Test]
    public async Task SetEnabled_TrueAfterFalse_RemovesIdentifier()
    {
        var directory = CreateTempDirectory();
        try
        {
            var store = new FilePluginEnabledStateStore(Path.Combine(directory, "disabled.txt"));
            store.SetEnabled("p", false);
            store.SetEnabled("p", true);

            await Assert.That(store.GetDisabledIdentifiers().Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that comments and blank lines in the file are ignored on load.
    /// </summary>
    [Test]
    public async Task Load_CommentsAndBlankLines_AreIgnored()
    {
        var directory = CreateTempDirectory();
        var filePath = Path.Combine(directory, "disabled.txt");
        try
        {
            File.WriteAllText(filePath, "# disabled list\n\nplugin.one\n# trailing comment\nplugin.two\n");
            var store = new FilePluginEnabledStateStore(filePath);

            var disabled = store.GetDisabledIdentifiers();
            await Assert.That(disabled.Count).IsEqualTo(2);
            await Assert.That(disabled.Contains("plugin.one")).IsTrue();
            await Assert.That(disabled.Contains("plugin.two")).IsTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that the store creates the parent directory on first save.
    /// </summary>
    [Test]
    public async Task SetEnabled_ParentDirectoryMissing_CreatesIt()
    {
        var directory = CreateTempDirectory();
        try
        {
            var nested = Path.Combine(directory, "nested", "deeper");
            var filePath = Path.Combine(nested, "disabled.txt");
            var store = new FilePluginEnabledStateStore(filePath);

            store.SetEnabled("p", false);

            await Assert.That(Directory.Exists(nested)).IsTrue();
            await Assert.That(File.Exists(filePath)).IsTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
