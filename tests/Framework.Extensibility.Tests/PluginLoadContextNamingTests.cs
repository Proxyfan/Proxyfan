using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginLoadContextNaming" />.
/// </summary>
public sealed class PluginLoadContextNamingTests
{
    /// <summary>
    ///     Verifies that the file name without extension is embedded in the load-context name.
    /// </summary>
    [Test]
    public async Task Build_TypicalPath_EmbedsFileNameWithoutExtension()
    {
        var name = PluginLoadContextNaming.Build(@"C:\plugins\Acme.MyPlugin\Acme.MyPlugin.dll");

        await Assert.That(name).IsEqualTo("PluginLoadContext(Acme.MyPlugin)");
    }

    /// <summary>
    ///     Verifies that paths with no extension still produce a valid name.
    /// </summary>
    [Test]
    public async Task Build_NoExtension_UsesFileNameAsIs()
    {
        var name = PluginLoadContextNaming.Build(@"C:\plugins\bare");

        await Assert.That(name).IsEqualTo("PluginLoadContext(bare)");
    }
}
