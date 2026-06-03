using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginUpdateManifestParser" />.
/// </summary>
public sealed class PluginUpdateManifestParserTests
{
    /// <summary>
    ///     A well-formed manifest parses into a populated entry list.
    /// </summary>
    [Test]
    public async Task TryParse_ValidManifest_ReturnsManifest()
    {
        var json = """
            {
              "plugins": [
                {
                  "id": "com.example.plugin",
                  "latestVersion": "2.1.0",
                  "downloadUrl": "https://example.com/plugin.zip",
                  "minApiVersion": "1.0"
                }
              ]
            }
            """;

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
        var manifest = PluginUpdateManifestParser.TryParse("""{ "other": 1 }""");

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A "plugins" property that is not an array returns null.
    /// </summary>
    [Test]
    public async Task TryParse_PluginsIsObject_ReturnsNull()
    {
        var manifest = PluginUpdateManifestParser.TryParse("""{ "plugins": { } }""");

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     Entries with missing id are skipped.
    /// </summary>
    [Test]
    public async Task TryParse_EntryMissingId_SkipsEntry()
    {
        var json = """
            {
              "plugins": [
                { "latestVersion": "1.0.0" },
                { "id": "com.example.ok", "latestVersion": "2.0.0", "downloadUrl": "https://example.com/ok.zip", "minApiVersion": "1.0" }
              ]
            }
            """;

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
        var json = """
            {
              "plugins": [
                { "id": "com.example.x" },
                { "id": "com.example.y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "minApiVersion": "1.0" }
              ]
            }
            """;

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
        var json = """
            {
              "plugins": [
                { "id": 123, "latestVersion": "1.0.0" }
              ]
            }
            """;

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
        var json = """
            {
              "plugins": [
                "string-element",
                42,
                { "id": "com.example.ok", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/ok.zip", "minApiVersion": "1.0" }
              ]
            }
            """;

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
        var json = """
            {
              "plugins": [
                { "id": "com.example.x", "latestVersion": "1.0.0", "minApiVersion": "1.0" },
                { "id": "com.example.y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "minApiVersion": "1.0" }
              ]
            }
            """;

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
        var json = """
            {
              "plugins": [
                { "id": "com.example.x", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/x.zip" },
                { "id": "com.example.y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "minApiVersion": "1.0" }
              ]
            }
            """;

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
        await Assert.That(manifest.Plugins[0].Identifier).IsEqualTo("com.example.y");
    }

    /// <summary>
    ///     Entries with blank downloadUrl or minApiVersion are skipped rather than defaulted.
    /// </summary>
    [Test]
    public async Task TryParse_BlankRequiredField_SkipsEntry()
    {
        var json = """
            {
              "plugins": [
                { "id": "x", "latestVersion": "1.0.0", "downloadUrl": "", "minApiVersion": "1.0" },
                { "id": "y", "latestVersion": "1.0.0", "downloadUrl": "https://example.com/y.zip", "minApiVersion": "   " }
              ]
            }
            """;

        var manifest = PluginUpdateManifestParser.TryParse(json);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).IsEmpty();
    }
}
