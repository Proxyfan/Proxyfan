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

    /// <summary>
    ///     Verifies that an empty preferences object (every field null) deserializes back to
    ///     the defaults, exercising the null branch of every <c>??</c> coalesce in
    ///     <see cref="UserPreferencesJsonSerializer.Deserialize" />.
    /// </summary>
    [Test]
    public async Task Deserialize_AllFieldsNull_AppliesEveryDefault()
    {
        var json = "{\"schemaVersion\":1,\"preferences\":{}}";

        var preferences = UserPreferencesJsonSerializer.Deserialize(json);

        var defaults = UserPreferencesDefaults.Create();
        await Assert.That(preferences.CaptureMaximumFlows).IsEqualTo(defaults.CaptureMaximumFlows);
        await Assert.That(preferences.IsRegisterSystemProxyOnStartup).IsEqualTo(defaults.IsRegisterSystemProxyOnStartup);
        await Assert.That(preferences.IsStartProxyOnLaunch).IsEqualTo(defaults.IsStartProxyOnLaunch);
        await Assert.That(preferences.IsUpstreamProxyEnabled).IsEqualTo(defaults.IsUpstreamProxyEnabled);
        await Assert.That(preferences.LogLevel).IsEqualTo(defaults.LogLevel);
        await Assert.That(preferences.ProxyPort).IsEqualTo(defaults.ProxyPort);
        await Assert.That(preferences.Theme).IsEqualTo(defaults.Theme);
        await Assert.That(preferences.UpstreamProxyPort).IsEqualTo(defaults.UpstreamProxyPort);
    }

    /// <summary>
    ///     Verifies that a payload missing the <c>preferences</c> property returns defaults.
    /// </summary>
    [Test]
    public async Task Deserialize_MissingPreferencesObject_ReturnsDefaults()
    {
        var json = "{\"schemaVersion\":1}";

        var preferences = UserPreferencesJsonSerializer.Deserialize(json);

        await Assert.That(preferences.ProxyPort).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that a persisted ProxyPort outside the 1024-65535 range is replaced with
    ///     the default rather than starting the listener on an invalid port.
    /// </summary>
    [Test]
    [Arguments(0)]
    [Arguments(80)]
    [Arguments(1023)]
    [Arguments(65536)]
    [Arguments(-1)]
    public async Task Deserialize_ProxyPortOutOfRange_FallsBackToDefault(int port)
    {
        var json = $"{{\"schemaVersion\":1,\"preferences\":{{\"proxyPort\":{port}}}}}";

        var preferences = UserPreferencesJsonSerializer.Deserialize(json);

        await Assert.That(preferences.ProxyPort).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that a persisted UpstreamProxyPort outside the 1-65535 range is replaced
    ///     with the default.
    /// </summary>
    [Test]
    [Arguments(0)]
    [Arguments(70000)]
    [Arguments(-5)]
    public async Task Deserialize_UpstreamProxyPortOutOfRange_FallsBackToDefault(int port)
    {
        var json = $"{{\"schemaVersion\":1,\"preferences\":{{\"upstreamProxyPort\":{port}}}}}";

        var preferences = UserPreferencesJsonSerializer.Deserialize(json);

        await Assert.That(preferences.UpstreamProxyPort).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that a persisted CaptureMaximumFlows below the minimum of 100 is replaced
    ///     with the default rather than starting with an unusably small capture cap.
    /// </summary>
    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(99)]
    [Arguments(-1)]
    public async Task Deserialize_CaptureMaximumFlowsBelowMinimum_FallsBackToDefault(int value)
    {
        var json = $"{{\"schemaVersion\":1,\"preferences\":{{\"captureMaximumFlows\":{value}}}}}";

        var preferences = UserPreferencesJsonSerializer.Deserialize(json);

        await Assert.That(preferences.CaptureMaximumFlows).IsEqualTo(10_000);
    }

    /// <summary>
    ///     Verifies that a persisted CaptureMaximumFlows above 1,000,000 is replaced with the
    ///     default so capture capacity stays within supported in-memory bounds.
    /// </summary>
    [Test]
    [Arguments(1_000_001)]
    [Arguments(2_000_000)]
    [Arguments(int.MaxValue)]
    public async Task Deserialize_CaptureMaximumFlowsAboveMaximum_FallsBackToDefault(int value)
    {
        var json = $"{{\"schemaVersion\":1,\"preferences\":{{\"captureMaximumFlows\":{value}}}}}";

        var preferences = UserPreferencesJsonSerializer.Deserialize(json);

        await Assert.That(preferences.CaptureMaximumFlows).IsEqualTo(10_000);
    }

    /// <summary>
    ///     Verifies that values at the edge of the valid ranges are preserved (boundary check).
    /// </summary>
    [Test]
    public async Task Deserialize_BoundaryValues_ArePreserved()
    {
        var json = "{\"schemaVersion\":1,\"preferences\":{\"proxyPort\":1024,\"upstreamProxyPort\":65535,\"captureMaximumFlows\":1000000}}";

        var preferences = UserPreferencesJsonSerializer.Deserialize(json);

        await Assert.That(preferences.ProxyPort).IsEqualTo(1024);
        await Assert.That(preferences.UpstreamProxyPort).IsEqualTo(65535);
        await Assert.That(preferences.CaptureMaximumFlows).IsEqualTo(1_000_000);
    }
}
