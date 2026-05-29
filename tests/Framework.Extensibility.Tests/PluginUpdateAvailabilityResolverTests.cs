using Proxyfan.Plugin.Abstractions;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginUpdateAvailabilityResolver" />.
/// </summary>
public sealed class PluginUpdateAvailabilityResolverTests
{
    /// <summary>
    ///     A null manifest yields an empty list.
    /// </summary>
    [Test]
    public async Task Resolve_NullManifest_ReturnsEmpty()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        registry.TryInitialize(CreateStubPlugin("a", "A", "1.0.0", "1.0"), host, null);

        var result = PluginUpdateAvailabilityResolver.Resolve(registry, null, "1.0");

        await Assert.That(result).IsEmpty();
    }

    /// <summary>
    ///     A registry whose plugin's version matches the manifest yields no upgrade.
    /// </summary>
    [Test]
    public async Task Resolve_SameVersion_ReturnsEmpty()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        registry.TryInitialize(CreateStubPlugin("com.x", "X", "1.0.0", "1.0"), host, null);
        var manifest = BuildManifest(("com.x", "1.0.0", "1.0", "https://example.com/x.zip"));

        var result = PluginUpdateAvailabilityResolver.Resolve(registry, manifest, "1.0");

        await Assert.That(result).IsEmpty();
    }

    /// <summary>
    ///     A manifest with a newer version produces one entry with compat flag set.
    /// </summary>
    [Test]
    public async Task Resolve_NewerVersionCompatible_EmitsEntry()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        registry.TryInitialize(CreateStubPlugin("com.x", "X", "1.0.0", "1.0"), host, null);
        var manifest = BuildManifest(("com.x", "1.1.0", "1.0", "https://example.com/x.zip"));

        var result = PluginUpdateAvailabilityResolver.Resolve(registry, manifest, "1.0");

        await Assert.That(result).Count().IsEqualTo(1);
        var availability = result[0];
        await Assert.That(availability.Identifier).IsEqualTo("com.x");
        await Assert.That(availability.CurrentVersion).IsEqualTo("1.0.0");
        await Assert.That(availability.LatestVersion).IsEqualTo("1.1.0");
        await Assert.That(availability.DownloadUrl).IsEqualTo("https://example.com/x.zip");
        await Assert.That(availability.IsCompatible).IsTrue();
    }

    /// <summary>
    ///     A manifest entry needing a newer host API marks the entry incompatible.
    /// </summary>
    [Test]
    public async Task Resolve_NewerVersionIncompatible_FlagsIncompat()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        registry.TryInitialize(CreateStubPlugin("com.x", "X", "1.0.0", "1.0"), host, null);
        var manifest = BuildManifest(("com.x", "2.0.0", "2.0", "https://example.com/x.zip"));

        var result = PluginUpdateAvailabilityResolver.Resolve(registry, manifest, "1.0");

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].IsCompatible).IsFalse();
    }

    /// <summary>
    ///     Plugins not present in the manifest are silently ignored.
    /// </summary>
    [Test]
    public async Task Resolve_NoManifestEntry_IgnoresPlugin()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        registry.TryInitialize(CreateStubPlugin("com.x", "X", "1.0.0", "1.0"), host, null);
        registry.TryInitialize(CreateStubPlugin("com.y", "Y", "1.0.0", "1.0"), host, null);
        var manifest = BuildManifest(("com.x", "1.1.0", "1.0", "https://example.com/x.zip"));

        var result = PluginUpdateAvailabilityResolver.Resolve(registry, manifest, "1.0");

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Identifier).IsEqualTo("com.x");
    }

    /// <summary>
    ///     Manifest entry identifier match is case-insensitive.
    /// </summary>
    [Test]
    public async Task Resolve_IdentifierCaseInsensitive_StillMatches()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        registry.TryInitialize(CreateStubPlugin("com.example.X", "X", "1.0.0", "1.0"), host, null);
        var manifest = BuildManifest(("COM.EXAMPLE.X", "1.1.0", "1.0", "https://example.com/x.zip"));

        var result = PluginUpdateAvailabilityResolver.Resolve(registry, manifest, "1.0");

        await Assert.That(result).Count().IsEqualTo(1);
    }

    /// <summary>
    ///     Manifest entries with blank identifiers are skipped during indexing.
    /// </summary>
    [Test]
    public async Task Resolve_BlankIdentifier_SkipsEntry()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0");
        registry.TryInitialize(CreateStubPlugin("com.x", "X", "1.0.0", "1.0"), host, null);
        var entry1 = new PluginUpdateEntry
        {
            Identifier = " ",
            LatestVersion = "9.9.9",
            DownloadUrl = "https://example.com/blank.zip",
            MinimumApiVersion = "1.0",
        };
        var entry2 = new PluginUpdateEntry
        {
            Identifier = "com.x",
            LatestVersion = "1.1.0",
            DownloadUrl = "https://example.com/x.zip",
            MinimumApiVersion = "1.0",
        };
        var manifest = new PluginUpdateManifest { Plugins = [entry1, entry2] };

        var result = PluginUpdateAvailabilityResolver.Resolve(registry, manifest, "1.0");

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Identifier).IsEqualTo("com.x");
    }

    private static StubPlugin CreateStubPlugin(string id, string name, string version, string apiVersion)
    {
        var plugin = new StubPlugin(id, name, version, "Author", "Description", apiVersion);
        return plugin;
    }

    private static PluginUpdateManifest BuildManifest(params (string Id, string Latest, string MinApi, string Url)[] rows)
    {
        var entries = new PluginUpdateEntry[rows.Length];
        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var entry = new PluginUpdateEntry
            {
                Identifier = row.Id,
                LatestVersion = row.Latest,
                DownloadUrl = row.Url,
                MinimumApiVersion = row.MinApi,
            };
            entries[index] = entry;
        }

        var manifest = new PluginUpdateManifest { Plugins = entries };
        return manifest;
    }

    private sealed class StubPlugin : IProxyfanPlugin
    {
        public StubPlugin(string id, string name, string version, string author, string description, string apiVersion)
        {
            Metadata = new PluginMetadata(id, name, version, author, description, apiVersion);
        }

        public PluginMetadata Metadata { get; }

        public void Initialize(IPluginHost host)
        {
        }
    }
}
