using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginLoadContextUnloader" />.
/// </summary>
public sealed class PluginLoadContextUnloaderTests
{
    /// <summary>
    ///     Verifies that unloading a freshly-created context completes without throwing.
    /// </summary>
    [Test]
    public async Task Unload_FreshContext_DoesNotThrow()
    {
        var hostAssemblyPath = typeof(PluginLoadContextUnloaderTests).Assembly.Location;
        var context = new PluginLoadContext(hostAssemblyPath);

        var thrown = CaptureException(() => PluginLoadContextUnloader.Unload(context));

        await Assert.That(thrown).IsNull();
    }

    /// <summary>
    ///     Verifies that unloading a context twice suppresses the second InvalidOperationException.
    /// </summary>
    [Test]
    public async Task Unload_AlreadyUnloaded_SwallowsException()
    {
        var hostAssemblyPath = typeof(PluginLoadContextUnloaderTests).Assembly.Location;
        var context = new PluginLoadContext(hostAssemblyPath);
        PluginLoadContextUnloader.Unload(context);

        var thrown = CaptureException(() => PluginLoadContextUnloader.Unload(context));

        await Assert.That(thrown).IsNull();
    }

    private static Exception? CaptureException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
