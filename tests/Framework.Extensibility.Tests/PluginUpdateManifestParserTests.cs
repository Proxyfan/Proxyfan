using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginUpdateManifestParser" />.
/// </summary>
public sealed class PluginUpdateManifestParserTests
{
    private const string ManifestSigningPrivateKeyPkcs8Base64 = "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgTQTs+TsFeht8UnXwDud1t16oDXCM/F75LAB7Wskxy8ehRANCAASz0xuLKTTtX0D67XWAn/7rX5vK+QkFPLS7hnEF6BdJb7mO+ule2ZkwPZFVRzHKH7U+VozJ+bejgI2ZhvE6aszd";

    /// <summary>
    ///     A well-formed signed manifest parses into a populated entry list.
    /// </summary>
    [Test]
    public async Task TryParse_ValidManifest_ReturnsManifest()
    {
        var pluginsJson = """
            [
              {
                "id": "com.example.plugin",
                "latestVersion": "2.1.0",
                "downloadUrl": "https://example.com/plugin.zip",
                "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                "minApiVersion": "1.0"
              }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
        var entry = manifest.Plugins[0];
        await Assert.That(entry.Identifier).IsEqualTo("com.example.plugin");
        await Assert.That(entry.LatestVersion).IsEqualTo("2.1.0");
        await Assert.That(entry.DownloadUrl).IsEqualTo("https://example.com/plugin.zip");
        await Assert.That(entry.MinimumApiVersion).IsEqualTo("1.0");
    }

    /// <summary>
    ///     Whitespace input returns null.
    /// </summary>
    [Test]
    public async Task TryParse_Whitespace_ReturnsNull()
    {
        var manifest = PluginUpdateManifestParser.TryParse("   ");

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     Null input returns null.
    /// </summary>
    [Test]
    public async Task TryParse_NullPayload_ReturnsNull()
    {
        var manifest = PluginUpdateManifestParser.TryParse(null);

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     Malformed JSON returns null instead of throwing.
    /// </summary>
    [Test]
    public async Task TryParse_MalformedJson_ReturnsNull()
    {
        var manifest = PluginUpdateManifestParser.TryParse("{ not valid json");

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A non-object root returns null.
    /// </summary>
    [Test]
    public async Task TryParse_RootIsArray_ReturnsNull()
    {
        var manifest = PluginUpdateManifestParser.TryParse("[ ]");

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A root without a "plugins" property returns null.
    /// </summary>
    [Test]
    public async Task TryParse_MissingPluginsArray_ReturnsNull()
    {
        var manifest = PluginUpdateManifestParser.TryParse("""{ "signature": "abc", "other": 1 }""");

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A "plugins" property that is not an array returns null.
    /// </summary>
    [Test]
    public async Task TryParse_PluginsIsObject_ReturnsNull()
    {
        var manifest = PluginUpdateManifestParser.TryParse("""{ "signature": "abc", "plugins": { } }""");

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     Unsigned manifests are rejected.
    /// </summary>
    [Test]
    public async Task TryParse_ManifestNotSigned_ReturnsNull()
    {
        var json = """
            {
              "plugins": [
                { "id": "com.example.x", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/x.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" }
              ]
            }
            """;

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     Entries with missing id are skipped.
    /// </summary>
    [Test]
    public async Task TryParse_EntryMissingId_SkipsEntry()
    {
        var pluginsJson = """
            [
              { "latestVersion": "1.0.0" },
              { "id": "com.example.ok", "latestVersion": "2.0.0", "downloadUrl": "https://example.com/ok.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
        await Assert.That(manifest.Plugins[0].Identifier).IsEqualTo("com.example.ok");
    }

    /// <summary>
    ///     Entries with missing latestVersion are skipped.
    /// </summary>
    [Test]
    public async Task TryParse_EntryMissingLatestVersion_SkipsEntry()
    {
        var pluginsJson = """
            [
              { "id": "com.example.x" },
              { "id": "com.example.y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
    }

    /// <summary>
    ///     Entries with non-string fields are skipped.
    /// </summary>
    [Test]
    public async Task TryParse_NonStringIdField_SkipsEntry()
    {
        var pluginsJson = """
            [
              { "id": 123, "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).IsEmpty();
    }

    /// <summary>
    ///     Non-object array elements are skipped.
    /// </summary>
    [Test]
    public async Task TryParse_NonObjectArrayElement_SkipsElement()
    {
        var pluginsJson = """
            [
              "string-element",
              42,
              { "id": "com.example.ok", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/ok.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
    }

    /// <summary>
    ///     Entries missing downloadUrl are skipped.
    /// </summary>
    [Test]
    public async Task TryParse_EntryMissingDownloadUrl_SkipsEntry()
    {
        var pluginsJson = """
            [
              { "id": "com.example.x", "latestVersion": "1.0.0", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" },
              { "id": "com.example.y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
        await Assert.That(manifest.Plugins[0].Identifier).IsEqualTo("com.example.y");
    }

    /// <summary>
    ///     Entries with a non-HTTPS download URL are skipped.
    /// </summary>
    [Test]
    public async Task TryParse_EntryDownloadUrlIsHttp_SkipsEntry()
    {
        var pluginsJson = """
            [
              { "id": "com.example.x", "latestVersion": "1.0.0", "downloadUrl": "http://example.com/x.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" },
              { "id": "com.example.y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
        await Assert.That(manifest.Plugins[0].Identifier).IsEqualTo("com.example.y");
    }

    /// <summary>
    ///     Entries missing minApiVersion are skipped.
    /// </summary>
    [Test]
    public async Task TryParse_EntryMissingMinApiVersion_SkipsEntry()
    {
        var pluginsJson = """
            [
              { "id": "com.example.x", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/x.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef" },
              { "id": "com.example.y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
        await Assert.That(manifest.Plugins[0].Identifier).IsEqualTo("com.example.y");
    }

    /// <summary>
    ///     Entries missing sha256 are skipped.
    /// </summary>
    [Test]
    public async Task TryParse_EntryMissingSha256_SkipsEntry()
    {
        var pluginsJson = """
            [
              { "id": "com.example.x", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/x.zip", "minApiVersion": "1.0" },
              { "id": "com.example.y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
        await Assert.That(manifest.Plugins[0].Identifier).IsEqualTo("com.example.y");
    }

    /// <summary>
    ///     Entries with blank required fields are skipped rather than defaulted.
    /// </summary>
    [Test]
    public async Task TryParse_BlankRequiredField_SkipsEntry()
    {
        var pluginsJson = """
            [
              { "id": "x", "latestVersion": "1.0.0", "downloadUrl": "", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "1.0" },
              { "id": "y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "sha256": "not-hex", "minApiVersion": "   " }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).IsEmpty();
    }

    private static string BuildSignedManifest(string pluginsJson)
    {
        var signature = Sign(pluginsJson);
        return $$"""
            {
              "signature": "{{signature}}",
              "plugins": {{pluginsJson}}
            }
            """;
    }

    private static string Sign(string pluginsJson)
    {
        var privateKeyBytes = Convert.FromBase64String(ManifestSigningPrivateKeyPkcs8Base64);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
        using var document = JsonDocument.Parse(pluginsJson);
        var signedPayload = document.RootElement.GetRawText();
        var payload = Encoding.UTF8.GetBytes(signedPayload);
        var signature = ecdsa.SignData(payload, HashAlgorithmName.SHA256);
        return Convert.ToBase64String(signature);
    }
}
