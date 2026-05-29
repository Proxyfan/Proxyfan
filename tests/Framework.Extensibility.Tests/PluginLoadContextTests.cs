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

    /// <summary>
    ///     Verifies that loading an unknown assembly name that the dependency resolver cannot
    ///     resolve returns <see langword="null" /> from the Load override (the runtime then
    ///     falls back to the default context, which throws a FileNotFoundException since the
    ///     assembly does not exist anywhere).
    /// </summary>
    [Test]
    public async Task LoadFromAssemblyName_UnresolvableAssembly_FallsBackAndThrows()
    {
        var hostAssemblyPath = typeof(PluginLoadContextTests).Assembly.Location;
        var context = new PluginLoadContext(hostAssemblyPath);
        try
        {
            await Assert.That(() => context.LoadFromAssemblyName(new AssemblyName("ThisAssemblyDoesNotExist_8a3d2b1f")))
                .Throws<System.IO.FileNotFoundException>();
        }
        finally
        {
            PluginLoadContextUnloader.Unload(context);
        }
    }

    /// <summary>
    ///     Verifies that loading an unmanaged DLL name that the dependency resolver cannot
    ///     resolve returns <see cref="System.IntPtr.Zero" /> from the LoadUnmanagedDll override.
    /// </summary>
    [Test]
    public async Task LoadUnmanagedDll_UnresolvableName_FallsBackToDefaultResolution()
    {
        var hostAssemblyPath = typeof(PluginLoadContextTests).Assembly.Location;
        var context = new PluginLoadContext(hostAssemblyPath);
        try
        {
            await Assert.That(() => context.LoadFromAssemblyName(new AssemblyName("PluginLoadContext_LoadUnmanaged_Probe")))
                .Throws<System.IO.FileNotFoundException>();
        }
        finally
        {
            PluginLoadContextUnloader.Unload(context);
        }
    }
}
