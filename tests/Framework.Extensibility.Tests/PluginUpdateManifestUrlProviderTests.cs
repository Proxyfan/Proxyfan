using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginUpdateManifestUrlProvider" />.
/// </summary>
public sealed class PluginUpdateManifestUrlProviderTests
{
    /// <summary>
    ///     An empty constructor argument yields not-configured state.
    /// </summary>
    [Test]
    public async Task HasConfiguration_EmptyUrl_ReturnsFalse()
    {
        var provider = new PluginUpdateManifestUrlProvider(string.Empty);

        await Assert.That(provider.HasConfiguration()).IsFalse();
    }

    /// <summary>
    ///     A whitespace-only URL yields not-configured state.
    /// </summary>
    [Test]
    public async Task HasConfiguration_WhitespaceUrl_ReturnsFalse()
    {
        var provider = new PluginUpdateManifestUrlProvider("   ");

        await Assert.That(provider.HasConfiguration()).IsFalse();
    }

    /// <summary>
    ///     A concrete URL yields configured state and round-trips through the getter.
    /// </summary>
    [Test]
    public async Task HasConfiguration_PopulatedUrl_ReturnsTrue()
    {
        var provider = new PluginUpdateManifestUrlProvider("https://example.com/manifest.json");

        await Assert.That(provider.HasConfiguration()).IsTrue();
        await Assert.That(provider.GetManifestUrl()).IsEqualTo("https://example.com/manifest.json");
    }

    /// <summary>
    ///     A null constructor argument is coerced to empty.
    /// </summary>
    [Test]
    public async Task GetManifestUrl_NullArg_ReturnsEmpty()
    {
        var provider = new PluginUpdateManifestUrlProvider(null!);

        await Assert.That(provider.HasConfiguration()).IsFalse();
        await Assert.That(provider.GetManifestUrl()).IsEqualTo(string.Empty);
    }
}
