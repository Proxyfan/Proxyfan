using Proxyfan.Plugin.Abstractions;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginRegistry" />.
/// </summary>
public sealed class PluginRegistryTests
{
    /// <summary>
    ///     Verifies that a freshly-constructed registry has no plugins.
    /// </summary>
    [Test]
    public async Task Plugins_Empty_IsEmpty()
    {
        var registry = new PluginRegistry();

        await Assert.That(registry.Plugins.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="PluginRegistry.TryInitialize" /> with a compatible plugin
    ///     loads it and registers via the host.
    /// </summary>
    [Test]
    public async Task TryInitialize_CompatiblePlugin_LoadsAndRegisters()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        var plugin = new StubPlugin("1.0", () => host.RegisterInspectorTab("Stub"));

        var loaded = registry.TryInitialize(plugin, host, null);

        await Assert.That(loaded.IsLoaded).IsTrue();
        await Assert.That(loaded.Instance).IsSameReferenceAs(plugin);
        await Assert.That(loaded.ErrorMessage).IsNull();
        await Assert.That(host.InspectorTabs.Count).IsEqualTo(1);
        await Assert.That(registry.Plugins.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that an incompatible API version is recorded with a failure.
    /// </summary>
    [Test]
    public async Task TryInitialize_IncompatibleApiVersion_RecordsFailure()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        var plugin = new StubPlugin("2.0", () => host.RegisterInspectorTab("ShouldNotRun"));

        var loaded = registry.TryInitialize(plugin, host, null);

        await Assert.That(loaded.IsLoaded).IsFalse();
        await Assert.That(loaded.Instance).IsNull();
        await Assert.That(loaded.ErrorMessage).Contains("incompatible");
        await Assert.That(host.InspectorTabs.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that an initialization exception is captured as ErrorMessage.
    /// </summary>
    [Test]
    public async Task TryInitialize_PluginThrowsDuringInit_CapturesError()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        var plugin = new StubPlugin("1.0", () => throw new InvalidOperationException("boom"));

        var loaded = registry.TryInitialize(plugin, host, null);

        await Assert.That(loaded.IsLoaded).IsFalse();
        await Assert.That(loaded.ErrorMessage).IsEqualTo("boom");
    }

    /// <summary>
    ///     Verifies that <see cref="PluginRegistry.Plugins" /> returns a detached snapshot
    ///     that cannot be cast back to the backing list and is not affected by subsequent
    ///     mutations.
    /// </summary>
    [Test]
    public async Task Plugins_AfterMutation_ReturnsDetachedSnapshot()
    {
        var registry = new PluginRegistry();
        var entry = new LoadedPlugin(new PluginMetadata("a", "A", "1.0", "Test", "desc", "1.0"), null, false, "err", null);
        registry.AddFailed(entry);

        var snapshot = registry.Plugins;
        registry.AddFailed(entry);

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(snapshot is System.Collections.Generic.List<LoadedPlugin>).IsFalse();
    }

    private sealed class StubPlugin : IProxyfanPlugin
    {
        private readonly Action _initAction;

        public PluginMetadata Metadata { get; }

        public StubPlugin(string apiVersion, Action initAction)
        {
            _initAction = initAction;
            Metadata = new PluginMetadata("stub", "Stub", "1.0", "Test", "Stub plugin", apiVersion);
        }

        public void Initialize(IPluginHost host)
        {
            _initAction();
        }
    }
}
