using Proxyfan.Domain.Configuration;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="UserPreferencesJsonSerializer" /> covering empty/invalid inputs and
///     full roundtrips of every field.
/// </summary>
public sealed class UserPreferencesJsonSerializerTests
{
    /// <summary>
    ///     Verifies that an empty input returns the defaults rather than throwing.
    /// </summary>
    [Test]
    public async Task Deserialize_EmptyJson_ReturnsDefaults()
    {
        var preferences = UserPreferencesJsonSerializer.Deserialize(string.Empty);

        await Assert.That(preferences.ProxyPort).IsEqualTo(8080);
        await Assert.That(preferences.Theme).IsEqualTo("System");
    }

    /// <summary>
    ///     Verifies that malformed JSON returns the defaults rather than throwing.
    /// </summary>
    [Test]
    public async Task Deserialize_MalformedJson_ReturnsDefaults()
    {
        var preferences = UserPreferencesJsonSerializer.Deserialize("{not-json");

        await Assert.That(preferences.ProxyPort).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that an unknown schema version falls back to defaults.
    /// </summary>
    [Test]
    public async Task Deserialize_UnknownSchemaVersion_ReturnsDefaults()
    {
        var json = "{\"schemaVersion\":999,\"preferences\":{\"proxyPort\":9999}}";

        var preferences = UserPreferencesJsonSerializer.Deserialize(json);

        await Assert.That(preferences.ProxyPort).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that every field roundtrips through serialize then deserialize.
    /// </summary>
    [Test]
    public async Task SerializeThenDeserialize_PopulatedPreferences_RoundtripsAllFields()
    {
        var original = new UserPreferences
        {
            CaptureMaximumFlows = 5000,
            IsRegisterSystemProxyOnStartup = false,
            IsStartProxyOnLaunch = false,
            IsUpstreamProxyEnabled = true,
            Locale = "fr-FR",
            LogLevel = "Debug",
            ProxyPort = 9090,
            Theme = "Dark",
            UpstreamProxyHost = "corp-proxy.example.com",
            UpstreamProxyPort = 3128,
        };

        var json = UserPreferencesJsonSerializer.Serialize(original);
        var loaded = UserPreferencesJsonSerializer.Deserialize(json);

        await Assert.That(loaded.CaptureMaximumFlows).IsEqualTo(5000);
        await Assert.That(loaded.IsRegisterSystemProxyOnStartup).IsFalse();
        await Assert.That(loaded.IsStartProxyOnLaunch).IsFalse();
        await Assert.That(loaded.IsUpstreamProxyEnabled).IsTrue();
        await Assert.That(loaded.Locale).IsEqualTo("fr-FR");
        await Assert.That(loaded.LogLevel).IsEqualTo("Debug");
        await Assert.That(loaded.ProxyPort).IsEqualTo(9090);
        await Assert.That(loaded.Theme).IsEqualTo("Dark");
        await Assert.That(loaded.UpstreamProxyHost).IsEqualTo("corp-proxy.example.com");
        await Assert.That(loaded.UpstreamProxyPort).IsEqualTo(3128);
    }
}
