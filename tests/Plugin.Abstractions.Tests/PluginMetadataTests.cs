using System.Threading.Tasks;

namespace Proxyfan.Plugin.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="PluginMetadata" />.
/// </summary>
public sealed class PluginMetadataTests
{
    /// <summary>
    ///     Verifies that the constructor stores all six metadata fields.
    /// </summary>
    [Test]
    public async Task Constructor_GivenAllFields_StoresAllValues()
    {
        var metadata = new PluginMetadata(
            id: "com.example.test",
            name: "Test Plugin",
            version: "1.0.0",
            author: "Test Author",
            description: "A test plugin",
            apiVersion: "1.0");

        await Assert.That(metadata.Id).IsEqualTo("com.example.test");
        await Assert.That(metadata.Name).IsEqualTo("Test Plugin");
        await Assert.That(metadata.Version).IsEqualTo("1.0.0");
        await Assert.That(metadata.Author).IsEqualTo("Test Author");
        await Assert.That(metadata.Description).IsEqualTo("A test plugin");
        await Assert.That(metadata.ApiVersion).IsEqualTo("1.0");
    }
}
