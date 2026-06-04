using System.Threading.Tasks;using Proxyfan.Plugin.Abstractions;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginApiVersionParser" />.
/// </summary>
public sealed class PluginApiVersionParserTests
{
    /// <summary>
    ///     A "major.minor" string parses into a <see cref="PluginApiVersion" /> with both components.
    /// </summary>
    [Test]
    public async Task Parse_MajorMinor_ReturnsVersion()
    {
        var version = PluginApiVersionParser.Parse("3.7");

        await Assert.That(version).IsNotNull();
        await Assert.That(version!.Major).IsEqualTo(3);
        await Assert.That(version.Minor).IsEqualTo(7);
    }

    /// <summary>
    ///     A "major.minor.patch" string parses, ignoring the patch component.
    /// </summary>
    [Test]
    public async Task Parse_MajorMinorPatch_IgnoresPatch()
    {
        var version = PluginApiVersionParser.Parse("1.2.99");

        await Assert.That(version).IsNotNull();
        await Assert.That(version!.Major).IsEqualTo(1);
        await Assert.That(version.Minor).IsEqualTo(2);
    }

    /// <summary>
    ///     A string with only one component (no dot) returns null.
    /// </summary>
    [Test]
    public async Task Parse_SingleComponent_ReturnsNull()
    {
        var version = PluginApiVersionParser.Parse("42");

        await Assert.That(version).IsNull();
    }

    /// <summary>
    ///     A non-numeric major component returns null, exercising the major-parse failure
    ///     branch in <see cref="PluginApiVersionParser.Parse" />.
    /// </summary>
    [Test]
    public async Task Parse_NonNumericMajor_ReturnsNull()
    {
        var version = PluginApiVersionParser.Parse("abc.5");

        await Assert.That(version).IsNull();
    }

    /// <summary>
    ///     A non-numeric minor component returns null.
    /// </summary>
    [Test]
    public async Task Parse_NonNumericMinor_ReturnsNull()
    {
        var version = PluginApiVersionParser.Parse("1.xyz");

        await Assert.That(version).IsNull();
    }

    /// <summary>
    ///     A negative major component returns null, since negative version numbers
    ///     violate SemVer and must not satisfy the compatibility gate.
    /// </summary>
    [Test]
    public async Task Parse_NegativeMajor_ReturnsNull()
    {
        var version = PluginApiVersionParser.Parse("-1.5");

        await Assert.That(version).IsNull();
    }

    /// <summary>
    ///     A negative minor component returns null, since negative version numbers
    ///     violate SemVer and must not satisfy the compatibility gate.
    /// </summary>
    [Test]
    public async Task Parse_NegativeMinor_ReturnsNull()
    {
        var version = PluginApiVersionParser.Parse("1.-1");

        await Assert.That(version).IsNull();
    }
}
