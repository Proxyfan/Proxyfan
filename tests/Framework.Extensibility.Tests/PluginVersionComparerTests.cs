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
    ///     Both versions are SemVer-shaped — fall back is not used.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_UnparseableCurrent_FallsBackToOrdinal()
    {
        var result = PluginVersionComparer.HasNewerCandidate("alpha", "beta");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     When one side parses as SemVer and the other does not, no update is
    ///     advertised — we refuse to compare apples to oranges rather than risk a
    ///     bogus downgrade.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_UnparseableCandidate_ReturnsFalse()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.0.0", "alpha");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Whitespace current with a SemVer candidate is asymmetric and therefore
    ///     does not advertise an update.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_WhitespaceCurrent_ReturnsFalse()
    {
        var result = PluginVersionComparer.HasNewerCandidate("   ", "1.0.0");

        await Assert.That(result).IsFalse();
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

    /// <summary>
    ///     A prerelease build has lower precedence than the corresponding stable
    ///     release (SemVer 2.0.0 §11.3).
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_PrereleaseCandidateVsStable_ReturnsFalse()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.2.3", "1.2.3-beta");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     A stable release is newer than the corresponding prerelease.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_StableCandidateVsPrerelease_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.2.3-beta", "1.2.3");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Prerelease identifiers compare in the SemVer-specified order:
    ///     alpha &lt; beta &lt; rc &lt; (stable).
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_PrereleaseAlphaVsBeta_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.2.3-alpha", "1.2.3-beta");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Numeric prerelease identifiers compare numerically, not lexically.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_NumericPrereleaseCompareNumerically_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.0.0-alpha.2", "1.0.0-alpha.10");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     A numeric prerelease identifier has lower precedence than an alphanumeric
    ///     identifier (SemVer 2.0.0 §11.4.3).
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_NumericIdentifierVsAlphanumeric_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.0.0-1", "1.0.0-alpha");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     A larger set of prerelease identifiers has higher precedence than a
    ///     smaller set when all preceding identifiers are equal (SemVer 2.0.0
    ///     §11.4.4).
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_LongerPrereleaseSet_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.0.0-alpha", "1.0.0-alpha.1");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Build metadata after <c>+</c> does not affect precedence — two versions
    ///     differing only in build metadata are equal.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_DifferentBuildMetadataOnly_ReturnsFalse()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.2.3+build.1", "1.2.3+build.2");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Build metadata is ignored when comparing prereleases.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_PrereleaseWithBuildMetadata_IgnoresBuild()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.0.0-rc.1+build.5", "1.0.0");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     A higher patch wins regardless of the prerelease on the lower-patch side.
    /// </summary>
    [Test]
    public async Task HasNewerCandidate_HigherPatchOverPrerelease_ReturnsTrue()
    {
        var result = PluginVersionComparer.HasNewerCandidate("1.2.3", "1.2.4-beta");

        await Assert.That(result).IsTrue();
    }
}
