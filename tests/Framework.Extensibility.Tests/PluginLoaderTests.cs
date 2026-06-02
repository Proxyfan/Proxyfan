using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginLoader" /> using on-disk candidate directories plus
///     stub instance factory and enabled-state store. The loader's job is orchestration
///     (scan + filter + factory + register), so these tests cover each branch through the
///     scan loop.
/// </summary>
[NotInParallel]
public sealed class PluginLoaderTests
{
    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-loader-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    private static void WriteManifest(string directory, string id)
    {
        File.WriteAllText(Path.Combine(directory, "plugin.manifest"), $"""
            id={id}
            name={id}
            version=1
            author=A
            description=D
            apiVersion=1.0
            assembly={id}.dll
            entryType={id}.Main
            """);
    }

    /// <summary>
    ///     Verifies that a successful plugin is initialised on the host.
    /// </summary>
    [Test]
    public async Task LoadAll_ValidEnabledPlugin_InitializesOnHost()
    {
        var root = CreateTempDirectory();
        try
        {
            var directory = Directory.CreateDirectory(Path.Combine(root, "p1"));
            WriteManifest(directory.FullName, "p1");
            var registry = new PluginRegistry();
            var host = new RecordingPluginHost("1.0");
            var loader = new PluginLoader(
                new PluginDirectoryScanner(),
                new StubInstanceFactory(c => PluginInstantiationResults.Success(new StubPlugin(c.Manifest!.Metadata, h => h.RegisterInspectorTab("t1")), null)),
                registry,
                new StubEnabledStateStore());

            var results = loader.LoadAll(root, host);

            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].IsLoaded).IsTrue();
            await Assert.That(host.InspectorTabs.Count).IsEqualTo(1);
            await Assert.That(registry.Plugins.Count).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that a disabled plugin is skipped and a failed entry is recorded.
    /// </summary>
    [Test]
    public async Task LoadAll_DisabledPlugin_RecordsFailedEntry()
    {
        var root = CreateTempDirectory();
        try
        {
            var directory = Directory.CreateDirectory(Path.Combine(root, "p1"));
            WriteManifest(directory.FullName, "p1");
            var registry = new PluginRegistry();
            var host = new RecordingPluginHost("1.0");
            var loader = new PluginLoader(
                new PluginDirectoryScanner(),
                new StubInstanceFactory(c => PluginInstantiationResults.Success(new StubPlugin(c.Manifest!.Metadata, _ => { }), null)),
                registry,
                new StubEnabledStateStore("p1"));

            var results = loader.LoadAll(root, host);

            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].IsLoaded).IsFalse();
            await Assert.That(results[0].ErrorMessage).IsEqualTo("Disabled by user.");
            await Assert.That(host.InspectorTabs.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that an invalid candidate (missing manifest) yields a failed entry with the parse error.
    /// </summary>
    [Test]
    public async Task LoadAll_InvalidCandidate_RecordsFailedEntry()
    {
        var root = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "broken"));
            var registry = new PluginRegistry();
            var host = new RecordingPluginHost("1.0");
            var loader = new PluginLoader(
                new PluginDirectoryScanner(),
                new StubInstanceFactory(_ => throw new InvalidOperationException("should not be called")),
                registry,
                new StubEnabledStateStore());

            var results = loader.LoadAll(root, host);

            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].IsLoaded).IsFalse();
            await Assert.That(results[0].ErrorMessage).Contains("Missing manifest");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that an instance-factory failure surfaces as a failed registry entry.
    /// </summary>
    [Test]
    public async Task LoadAll_InstanceFactoryFailure_RecordsFailedEntry()
    {
        var root = CreateTempDirectory();
        try
        {
            var directory = Directory.CreateDirectory(Path.Combine(root, "p1"));
            WriteManifest(directory.FullName, "p1");
            var registry = new PluginRegistry();
            var host = new RecordingPluginHost("1.0");
            var loader = new PluginLoader(
                new PluginDirectoryScanner(),
                new StubInstanceFactory(_ => PluginInstantiationResults.Failure("activator boom")),
                registry,
                new StubEnabledStateStore());

            var results = loader.LoadAll(root, host);

            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].IsLoaded).IsFalse();
            await Assert.That(results[0].ErrorMessage).IsEqualTo("activator boom");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that a plugin whose runtime metadata id differs from the manifest id is
    ///     rejected without being initialised on the host. Also exercises the case where the
    ///     runtime id is in the disabled set (and the manifest id is not), so a stale or
    ///     tampered manifest cannot bypass the user's disable choice.
    /// </summary>
    [Test]
    public async Task LoadAll_RuntimeIdDiffersFromManifestId_RecordsFailedEntry()
    {
        var root = CreateTempDirectory();
        try
        {
            var directory = Directory.CreateDirectory(Path.Combine(root, "p1"));
            WriteManifest(directory.FullName, "manifest-id");
            var registry = new PluginRegistry();
            var host = new RecordingPluginHost("1.0");
            var spoofedMetadata = new PluginMetadata("runtime-id", "runtime", "1", "A", "D", "1.0");
            var loader = new PluginLoader(
                new PluginDirectoryScanner(),
                new StubInstanceFactory(_ => PluginInstantiationResults.Success(new StubPlugin(spoofedMetadata, h => h.RegisterInspectorTab("should-not-register")), null)),
                registry,
                new StubEnabledStateStore("runtime-id"));

            var results = loader.LoadAll(root, host);

            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].IsLoaded).IsFalse();
            await Assert.That(results[0].ErrorMessage).Contains("runtime-id");
            await Assert.That(results[0].ErrorMessage).Contains("manifest-id");
            await Assert.That(host.InspectorTabs.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubInstanceFactory : IPluginInstanceFactory
    {
        private readonly Func<PluginCandidate, PluginInstantiationResult> _create;

        public StubInstanceFactory(Func<PluginCandidate, PluginInstantiationResult> create)
        {
            _create = create;
        }

        public PluginInstantiationResult Create(PluginCandidate candidate)
        {
            return _create(candidate);
        }
    }

    private sealed class StubEnabledStateStore : IPluginEnabledStateStore
    {
        private readonly HashSet<string> _disabled;

        public StubEnabledStateStore(params string[] disabled)
        {
            _disabled = new HashSet<string>(disabled.ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlySet<string> GetDisabledIdentifiers()
        {
            return _disabled;
        }

        public void SetEnabled(string identifier, bool isEnabled)
        {
        }
    }

    private sealed class StubPlugin : IProxyfanPlugin
    {
        private readonly Action<IPluginHost> _initAction;

        public PluginMetadata Metadata { get; }

        public StubPlugin(PluginMetadata metadata, Action<IPluginHost> initAction)
        {
            Metadata = metadata;
            _initAction = initAction;
        }

        public void Initialize(IPluginHost host)
        {
            _initAction(host);
        }
    }
}
