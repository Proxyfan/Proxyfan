using Proxyfan.Plugin.Abstractions;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="LoadedPlugin" />.
/// </summary>
public sealed class LoadedPluginTests
{
    /// <summary>
    ///     Verifies that all five constructor parameters are surfaced on the corresponding properties.
    /// </summary>
    [Test]
    public async Task Constructor_AllArguments_PopulatesAllProperties()
    {
        var metadata = new PluginMetadata("id", "n", "v", "a", "d", "1.0");
        var instance = new StubPlugin(metadata);

        var loaded = new LoadedPlugin(metadata, instance, true, null, @"C:\plugins\id");

        await Assert.That(loaded.Metadata).IsSameReferenceAs(metadata);
        await Assert.That(loaded.Instance).IsSameReferenceAs(instance);
        await Assert.That(loaded.IsLoaded).IsTrue();
        await Assert.That(loaded.ErrorMessage).IsNull();
        await Assert.That(loaded.SourceDirectory).IsEqualTo(@"C:\plugins\id");
    }

    /// <summary>
    ///     Verifies that a failure entry stores its error message.
    /// </summary>
    [Test]
    public async Task Constructor_FailedLoad_RetainsErrorMessage()
    {
        var metadata = new PluginMetadata("id", "n", "v", "a", "d", "1.0");

        var loaded = new LoadedPlugin(metadata, null, false, "boom", null);

        await Assert.That(loaded.IsLoaded).IsFalse();
        await Assert.That(loaded.Instance).IsNull();
        await Assert.That(loaded.ErrorMessage).IsEqualTo("boom");
        await Assert.That(loaded.SourceDirectory).IsNull();
    }

    private sealed class StubPlugin : IProxyfanPlugin
    {
        public PluginMetadata Metadata { get; }

        public StubPlugin(PluginMetadata metadata)
        {
            Metadata = metadata;
        }

        public void Initialize(IPluginHost host)
        {
        }
    }
}
