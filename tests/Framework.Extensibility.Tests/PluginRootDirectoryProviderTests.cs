using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginRootDirectoryProvider" />.
/// </summary>
public sealed class PluginRootDirectoryProviderTests
{
    /// <summary>
    ///     Verifies that GetRootDirectory returns the value supplied at construction time.
    /// </summary>
    [Test]
    public async Task GetRootDirectory_AfterConstruction_ReturnsSuppliedValue()
    {
        var provider = new PluginRootDirectoryProvider(@"C:\plugins\root");

        var rootDirectory = provider.GetRootDirectory();

        await Assert.That(rootDirectory).IsEqualTo(@"C:\plugins\root");
    }
}
