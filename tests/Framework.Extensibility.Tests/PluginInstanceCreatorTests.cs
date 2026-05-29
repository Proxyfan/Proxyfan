using Proxyfan.Plugin.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for the static helpers in <see cref="PluginInstanceCreator" /> that do not
///     require a custom plugin DLL on disk.
/// </summary>
[NotInParallel]
public sealed class PluginInstanceCreatorTests
{
    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-creator-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    private static void BestEffortDelete(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }

    /// <summary>
    ///     Verifies that CreateContext succeeds for an arbitrary existing assembly file path.
    /// </summary>
    [Test]
    public async Task CreateContext_GivenHostAssemblyPath_ReturnsSuccess()
    {
        var hostAssemblyPath = typeof(PluginInstanceCreatorTests).Assembly.Location;

        var result = PluginInstanceCreator.CreateContext(hostAssemblyPath);
        try
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Context).IsNotNull();
        }
        finally
        {
            if (result.Context is not null)
            {
                PluginLoadContextUnloader.Unload(result.Context);
            }
        }
    }

    /// <summary>
    ///     Verifies that InstantiateFromContext fails when the entry type does not exist.
    /// </summary>
    [Test]
    public async Task InstantiateFromContext_MissingEntryType_ReturnsFailure()
    {
        var hostAssemblyPath = typeof(PluginInstanceCreatorTests).Assembly.Location;
        var directory = CreateTempDirectory();
        try
        {
            var stagedAssemblyPath = Path.Combine(directory, Path.GetFileName(hostAssemblyPath));
            File.Copy(hostAssemblyPath, stagedAssemblyPath);
            var context = new PluginLoadContext(stagedAssemblyPath);
            var metadata = new PluginMetadata("p", "P", "1", "A", "D", "1.0");
            var manifest = new PluginManifest(metadata, Path.GetFileName(hostAssemblyPath), "DoesNot.Exist");

            var result = PluginInstanceCreator.InstantiateFromContext(context, stagedAssemblyPath, manifest);

            await Assert.That(result.IsSuccess).IsFalse();
            await Assert.That(result.ErrorMessage).Contains("Entry type");

            PluginLoadContextUnloader.Unload(context);
        }
        finally
        {
            BestEffortDelete(directory);
        }
    }

    /// <summary>
    ///     Verifies that InstantiateFromContext fails when the entry type does not implement IProxyfanPlugin.
    /// </summary>
    [Test]
    public async Task InstantiateFromContext_EntryTypeNotPlugin_ReturnsFailure()
    {
        var hostAssemblyPath = typeof(PluginInstanceCreatorTests).Assembly.Location;
        var directory = CreateTempDirectory();
        try
        {
            var stagedAssemblyPath = Path.Combine(directory, Path.GetFileName(hostAssemblyPath));
            File.Copy(hostAssemblyPath, stagedAssemblyPath);
            var context = new PluginLoadContext(stagedAssemblyPath);
            var metadata = new PluginMetadata("p", "P", "1", "A", "D", "1.0");
            var manifest = new PluginManifest(metadata, Path.GetFileName(hostAssemblyPath), typeof(PluginInstanceCreatorTests).FullName!);

            var result = PluginInstanceCreator.InstantiateFromContext(context, stagedAssemblyPath, manifest);

            await Assert.That(result.IsSuccess).IsFalse();
            await Assert.That(result.ErrorMessage).Contains("does not implement");

            PluginLoadContextUnloader.Unload(context);
        }
        finally
        {
            BestEffortDelete(directory);
        }
    }

    /// <summary>
    ///     Verifies that InstantiateFromContext succeeds when the entry type is a public
    ///     IProxyfanPlugin with a parameterless constructor.
    /// </summary>
    [Test]
    public async Task InstantiateFromContext_ValidPluginEntryType_ReturnsSuccess()
    {
        var hostAssemblyPath = typeof(PluginInstanceCreatorTests).Assembly.Location;
        var directory = CreateTempDirectory();
        try
        {
            var stagedAssemblyPath = Path.Combine(directory, Path.GetFileName(hostAssemblyPath));
            File.Copy(hostAssemblyPath, stagedAssemblyPath);
            var context = new PluginLoadContext(stagedAssemblyPath);
            var metadata = new PluginMetadata("p", "P", "1", "A", "D", "1.0");
            var entryTypeName = typeof(Proxyfan.Framework.Extensibility.Tests.Stubs.TestPlugin).FullName!;
            var manifest = new PluginManifest(metadata, Path.GetFileName(hostAssemblyPath), entryTypeName);

            var result = PluginInstanceCreator.InstantiateFromContext(context, stagedAssemblyPath, manifest);

            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Plugin).IsNotNull();
            await Assert.That(result.LoadContext).IsSameReferenceAs(context);

            PluginLoadContextUnloader.Unload(context);
        }
        finally
        {
            BestEffortDelete(directory);
        }
    }

    /// <summary>
    ///     Verifies that InstantiateFromContext fails when LoadFromAssemblyPath throws (the
    ///     file is not a valid PE assembly).
    /// </summary>
    [Test]
    public async Task InstantiateFromContext_InvalidAssemblyFile_ReturnsFailure()
    {
        var directory = CreateTempDirectory();
        try
        {
            var fakePath = Path.Combine(directory, "Garbage.dll");
            File.WriteAllText(fakePath, "not a real assembly file");
            var context = new PluginLoadContext(fakePath);
            var metadata = new PluginMetadata("p", "P", "1", "A", "D", "1.0");
            var manifest = new PluginManifest(metadata, "Garbage.dll", "Some.Type");

            var result = PluginInstanceCreator.InstantiateFromContext(context, fakePath, manifest);

            await Assert.That(result.IsSuccess).IsFalse();
            await Assert.That(result.ErrorMessage).Contains("Failed to load assembly");
        }
        finally
        {
            BestEffortDelete(directory);
        }
    }
}
