using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginApiVersionChecker" />.
/// </summary>
public sealed class PluginApiVersionCheckerTests
{
    /// <summary>
    ///     Verifies that an exact match is compatible.
    /// </summary>
    [Test]
    public async Task HasCompatibility_ExactMatch_ReturnsTrue()
    {
        var result = PluginApiVersionChecker.HasCompatibility("1.5.0", "1.5.0");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that host minor newer than plugin's minor is compatible.
    /// </summary>
    [Test]
    public async Task HasCompatibility_HostMinorNewer_ReturnsTrue()
    {
        var result = PluginApiVersionChecker.HasCompatibility("1.5.0", "1.3.0");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that host minor older than plugin's minor is incompatible.
    /// </summary>
    [Test]
    public async Task HasCompatibility_HostMinorOlder_ReturnsFalse()
    {
        var result = PluginApiVersionChecker.HasCompatibility("1.3.0", "1.5.0");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that different majors are always incompatible.
    /// </summary>
    [Test]
    public async Task HasCompatibility_DifferentMajors_ReturnsFalse()
    {
        var result = PluginApiVersionChecker.HasCompatibility("2.0.0", "1.0.0");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that an unparseable host version returns false.
    /// </summary>
    [Test]
    public async Task HasCompatibility_UnparsableHostVersion_ReturnsFalse()
    {
        var result = PluginApiVersionChecker.HasCompatibility("invalid", "1.0.0");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that an unparseable plugin version returns false.
    /// </summary>
    [Test]
    public async Task HasCompatibility_UnparsablePluginVersion_ReturnsFalse()
    {
        var result = PluginApiVersionChecker.HasCompatibility("1.0.0", "invalid");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that a version with only a major part (no minor) returns false.
    /// </summary>
    [Test]
    public async Task HasCompatibility_SinglePartVersion_ReturnsFalse()
    {
        var result = PluginApiVersionChecker.HasCompatibility("1", "1.0");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that a non-numeric minor returns false.
    /// </summary>
    [Test]
    public async Task HasCompatibility_NonNumericMinor_ReturnsFalse()
    {
        var result = PluginApiVersionChecker.HasCompatibility("1.x", "1.0");

        await Assert.That(result).IsFalse();
    }
}
