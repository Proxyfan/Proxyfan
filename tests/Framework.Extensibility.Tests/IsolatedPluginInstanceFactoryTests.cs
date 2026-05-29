using Proxyfan.Plugin.Abstractions;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="IsolatedPluginInstanceFactory" /> covering the validation and
///     resolution branches that do not require a real plugin DLL on disk.
/// </summary>
[NotInParallel]
public sealed class IsolatedPluginInstanceFactoryTests
{
    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-iso-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    ///     Verifies that an invalid candidate fails immediately with the candidate error.
    /// </summary>
    [Test]
    public async Task Create_InvalidCandidate_ReturnsFailure()
    {
        var factory = new IsolatedPluginInstanceFactory();
        var candidate = PluginCandidates.Invalid(@"C:\plugins\bad", "broken manifest");

        var result = factory.Create(candidate);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.ErrorMessage).IsEqualTo("broken manifest");
    }

    /// <summary>
    ///     Verifies that a valid candidate pointing at a non-existent assembly file fails.
    /// </summary>
    [Test]
    public async Task Create_MissingAssemblyFile_ReturnsFailure()
    {
        var factory = new IsolatedPluginInstanceFactory();
        var directory = CreateTempDirectory();
        try
        {
            var metadata = new PluginMetadata("p", "P", "1", "A", "D", "1.0");
            var manifest = new PluginManifest(metadata, "Missing.dll", "P.Main");
            var candidate = PluginCandidates.Valid(directory, manifest);

            var result = factory.Create(candidate);

            await Assert.That(result.IsSuccess).IsFalse();
            await Assert.That(result.ErrorMessage).Contains("Missing.dll");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that a non-DLL file (text) loaded as an assembly produces a failure.
    /// </summary>
    [Test]
    public async Task Create_NonAssemblyFile_ReturnsFailure()
    {
        var factory = new IsolatedPluginInstanceFactory();
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "Fake.dll"), "not a real assembly");
            var metadata = new PluginMetadata("p", "P", "1", "A", "D", "1.0");
            var manifest = new PluginManifest(metadata, "Fake.dll", "P.Main");
            var candidate = PluginCandidates.Valid(directory, manifest);

            var result = factory.Create(candidate);

            await Assert.That(result.IsSuccess).IsFalse();
            await Assert.That(result.ErrorMessage).IsNotNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that a valid candidate whose manifest references the test assembly's
    ///     <c>TestPlugin</c> type produces a fully-instantiated plugin.
    /// </summary>
    [Test]
    public async Task Create_ValidAssemblyAndPluginEntryType_ReturnsLoadedPluginInstance()
    {
        var factory = new IsolatedPluginInstanceFactory();
        var directory = CreateTempDirectory();
        try
        {
            var hostAssemblyPath = typeof(IsolatedPluginInstanceFactoryTests).Assembly.Location;
            var stagedFileName = "PluginUnderTest_" + Path.GetRandomFileName() + ".dll";
            File.Copy(hostAssemblyPath, Path.Combine(directory, stagedFileName));
            var metadata = new PluginMetadata("p", "P", "1", "A", "D", "1.0");
            var entryTypeName = typeof(Stubs.TestPlugin).FullName!;
            var manifest = new PluginManifest(metadata, stagedFileName, entryTypeName);
            var candidate = PluginCandidates.Valid(directory, manifest);

            var result = factory.Create(candidate);

            try
            {
                await Assert.That(result.IsSuccess).IsTrue();
                await Assert.That(result.Plugin).IsNotNull();
                await Assert.That(result.LoadContext).IsNotNull();
            }
            finally
            {
                if (result.LoadContext is PluginLoadContext pluginContext)
                {
                    PluginLoadContextUnloader.Unload(pluginContext);
                }
            }
        }
        finally
        {
            BestEffortDelete(directory);
        }
    }

    private static void BestEffortDelete(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (System.IO.IOException)
        {
        }
        catch (System.UnauthorizedAccessException)
        {
        }
    }
}
