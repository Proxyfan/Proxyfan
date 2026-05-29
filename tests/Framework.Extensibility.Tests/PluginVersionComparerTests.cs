using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginVersionComparer" />.
/// </summary>
public sealed class PluginVersionComparerTests
{
    /// <summary>
    ///     A higher major version is newer.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_HigherMajor_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.0.0", "2.0.0");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     A lower major version is not newer.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_LowerMajor_ReturnsFalse()
    {
        var result = PluginVersionComparer.HasNewerCandidate("2.0.0", "1.9.9");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Same major + higher minor is newer.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_HigherMinor_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.2.0", "1.3.0");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Same major + lower minor is not newer.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_LowerMinor_ReturnsFalse()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.5.0", "1.3.9");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Same major + minor + higher patch is newer.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_HigherPatch_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.2.3", "1.2.4");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Identical versions are not newer.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_Equal_ReturnsFalse()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.2.3", "1.2.3");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Two-component versions parse and compare correctly.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_TwoComponentVersions_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.2", "1.3");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Single-component versions parse and compare correctly.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_SingleComponent_ComparesNumerically()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1", "2");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Unparseable versions fall back to ordinal comparison.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_UnparseableCurrent_FallsBackToOrdinal()
    {
        var result = PluginVersionComparer.HasNewerCandidate("alpha", "beta");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Unparseable candidate version uses ordinal fallback.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_UnparseableCandidate_FallsBackToOrdinal()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.0.0", "alpha");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Whitespace-only versions are unparseable and use ordinal fallback.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_WhitespaceCurrent_UsesOrdinal()
    {
        var result = PluginVersionComparer.HasNewerCandidate("   ", "1.0.0");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     A patch bump from .0 to .1 is detected.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_PatchBumpFromZero_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.0.0", "1.0.1");

        await Assert.That(result).IsTrue();
    }
}
