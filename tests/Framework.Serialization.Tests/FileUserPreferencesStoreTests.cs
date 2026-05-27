using Proxyfan.Domain.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="FileUserPreferencesStore" /> covering missing-file load and round
///     trips via the file system.
/// </summary>
public sealed class FileUserPreferencesStoreTests
{
    /// <summary>
    ///     Verifies that loading from a non-existent path returns the defaults.
    /// </summary>
    [Test]
    public async Task Load_MissingFile_ReturnsDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-prefs-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "preferences.json");
        var store = new FileUserPreferencesStore(path);

        var preferences = store.Load();

        await Assert.That(preferences.ProxyPort).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that <see cref="FileUserPreferencesStore.Save" /> followed by
    ///     <see cref="FileUserPreferencesStore.Load" /> returns the same preferences via the
    ///     file system.
    /// </summary>
    [Test]
    public async Task SaveThenLoad_RoundTripFile_ReturnsSamePreferences()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-prefs-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "preferences.json");
        var store = new FileUserPreferencesStore(path);

        try
        {
            var original = new UserPreferences
            {
                CaptureMaximumFlows = 4242,
                IsRegisterSystemProxyOnStartup = false,
                IsStartProxyOnLaunch = false,
                IsUpstreamProxyEnabled = true,
                Locale = "es-ES",
                LogLevel = "Trace",
                ProxyPort = 7070,
                Theme = "Light",
                UpstreamProxyHost = "proxy.example.com",
                UpstreamProxyPort = 9999,
            };
            store.Save(original);

            var loaded = store.Load();

            await Assert.That(loaded.ProxyPort).IsEqualTo(7070);
            await Assert.That(loaded.UpstreamProxyHost).IsEqualTo("proxy.example.com");
            await Assert.That(loaded.Theme).IsEqualTo("Light");
            await Assert.That(loaded.IsUpstreamProxyEnabled).IsTrue();
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
