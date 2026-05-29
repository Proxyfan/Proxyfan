using System.Reflection;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginSharedContractAssemblies" />.
/// </summary>
public sealed class PluginSharedContractAssembliesTests
{
    /// <summary>
    ///     Verifies that the abstractions short name matches.
    /// </summary>
    [Test]
    public async Task HasMatch_ShortName_ReturnsTrue()
    {
        var name = new AssemblyName("Plugin.Abstractions");

        var matched = PluginSharedContractAssemblies.HasMatch(name);

        await Assert.That(matched).IsTrue();
    }

    /// <summary>
    ///     Verifies that the abstractions full name matches.
    /// </summary>
    [Test]
    public async Task HasMatch_FullName_ReturnsTrue()
    {
        var name = new AssemblyName("Proxyfan.Plugin.Abstractions");

        var matched = PluginSharedContractAssemblies.HasMatch(name);

        await Assert.That(matched).IsTrue();
    }

    /// <summary>
    ///     Verifies that unrelated assembly names do not match.
    /// </summary>
    [Test]
    public async Task HasMatch_UnrelatedAssembly_ReturnsFalse()
    {
        var name = new AssemblyName("System.Text.Json");

        var matched = PluginSharedContractAssemblies.HasMatch(name);

        await Assert.That(matched).IsFalse();
    }
}
