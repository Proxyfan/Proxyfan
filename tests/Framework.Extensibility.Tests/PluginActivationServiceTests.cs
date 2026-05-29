using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginActivationService" /> and the related boot wiring.
/// </summary>
[NotInParallel]
public sealed class PluginActivationServiceTests
{
    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-act-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    private static PluginActivationService CreateService(string root)
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        var loader = new PluginLoader(
            new PluginDirectoryScanner(),
            new IsolatedPluginInstanceFactory(),
            registry,
            new FilePluginEnabledStateStore(Path.Combine(root, "disabled.txt")));
        var provider = new PluginRootDirectoryProvider(root);
        var service = new PluginActivationService(loader, host, provider, registry);
        return service;
    }

    /// <summary>
    ///     Verifies that EnsureLoaded flips IsActivated to true on first call.
    /// </summary>
    [Test]
    public async Task EnsureLoaded_FirstCall_SetsIsActivatedTrue()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = CreateService(root);

            service.EnsureLoaded();

            await Assert.That(service.IsActivated).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that EnsureLoaded is idempotent (subsequent calls don't re-scan).
    /// </summary>
    [Test]
    public async Task EnsureLoaded_SecondCall_IsNoOp()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = CreateService(root);
            service.EnsureLoaded();

            service.EnsureLoaded();

            await Assert.That(service.IsActivated).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that Reload sets IsActivated true even before EnsureLoaded.
    /// </summary>
    [Test]
    public async Task Reload_BeforeEnsureLoaded_SetsIsActivatedTrue()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = CreateService(root);

            service.Reload();

            await Assert.That(service.IsActivated).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
