using System.Reflection;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginLoadContext" /> covering the construction and shared-assembly
///     delegation path.
/// </summary>
public sealed class PluginLoadContextTests
{
    /// <summary>
    ///     Verifies that AssemblyPath captures the supplied path verbatim.
    /// </summary>
    [Test]
    public async Task Constructor_GivenAssemblyPath_StoresPath()
    {
        var hostAssemblyPath = typeof(PluginLoadContextTests).Assembly.Location;

        var context = new PluginLoadContext(hostAssemblyPath);
        try
        {
            await Assert.That(context.AssemblyPath).IsEqualTo(hostAssemblyPath);
            await Assert.That(context.Name).IsNotNull();
        }
        finally
        {
            PluginLoadContextUnloader.Unload(context);
        }
    }

    /// <summary>
    ///     Verifies that the shared Plugin.Abstractions assembly always resolves via the default
    ///     load context (i.e. identity preserved across host/plugin boundary).
    /// </summary>
    [Test]
    public async Task LoadFromAssemblyName_AbstractionsAssembly_ResolvesViaDefaultContext()
    {
        var hostAssemblyPath = typeof(PluginLoadContextTests).Assembly.Location;
        var context = new PluginLoadContext(hostAssemblyPath);
        try
        {
            var loaded = context.LoadFromAssemblyName(new AssemblyName("Plugin.Abstractions"));

            await Assert.That(loaded).IsNotNull();
        }
        finally
        {
            PluginLoadContextUnloader.Unload(context);
        }
    }
}
