using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Configuration.Migration;

namespace Proxyfan.Domain.Configuration.Tests.Migration;

/// <summary>
///     Tests for <see cref="ConfigurationVersion" />.
/// </summary>
public sealed class ConfigurationVersionTests
{
    /// <summary>
    ///     Verifies the default-constructed struct has both components equal to zero.
    /// </summary>
    [Test]
    public async Task Default_NoArguments_HasZeroComponents()
    {
        var version = default(ConfigurationVersion);

        await Assert.That(version.Major).IsEqualTo(0);
        await Assert.That(version.Minor).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies the public constructor stores the supplied components verbatim.
    /// </summary>
    [Test]
    public async Task Construction_ValidComponents_StoresMajorAndMinor()
    {
        var version = new ConfigurationVersion(2, 5);

        await Assert.That(version.Major).IsEqualTo(2);
        await Assert.That(version.Minor).IsEqualTo(5);
    }

    /// <summary>
    ///     Verifies a negative major component throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [Test]
    public async Task Construction_NegativeMajor_Throws()
    {
        await Assert.That(() => new ConfigurationVersion(-1, 0))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies a negative minor component throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [Test]
    public async Task Construction_NegativeMinor_Throws()
    {
        await Assert.That(() => new ConfigurationVersion(1, -1))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies <see cref="ConfigurationVersion.Parse" /> accepts valid <c>Major.Minor</c>.
    /// </summary>
    [Test]
    public async Task Parse_ValidText_ReturnsParsedVersion()
    {
        var version = ConfigurationVersion.Parse("3.14");

        await Assert.That(version.Major).IsEqualTo(3);
        await Assert.That(version.Minor).IsEqualTo(14);
    }

    /// <summary>
    ///     Verifies blank text triggers <see cref="ArgumentException" />.
    /// </summary>
    [Test]
    public async Task Parse_BlankText_Throws()
    {
        await Assert.That(() => ConfigurationVersion.Parse("   ")).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies text missing the separator throws <see cref="FormatException" />.
    /// </summary>
    [Test]
    public async Task Parse_MissingSeparator_Throws()
    {
        await Assert.That(() => ConfigurationVersion.Parse("1")).Throws<FormatException>();
    }

    /// <summary>
    ///     Verifies text with a trailing separator throws <see cref="FormatException" />.
    /// </summary>
    [Test]
    public async Task Parse_TrailingSeparator_Throws()
    {
        await Assert.That(() => ConfigurationVersion.Parse("1.")).Throws<FormatException>();
    }

    /// <summary>
    ///     Verifies text with a leading separator throws <see cref="FormatException" />.
    /// </summary>
    [Test]
    public async Task Parse_LeadingSeparator_Throws()
    {
        await Assert.That(() => ConfigurationVersion.Parse(".5")).Throws<FormatException>();
    }

    /// <summary>
    ///     Verifies non-integer components throw <see cref="FormatException" />.
    /// </summary>
    [Test]
    public async Task Parse_NonIntegerComponents_Throws()
    {
        await Assert.That(() => ConfigurationVersion.Parse("a.b")).Throws<FormatException>();
    }

    /// <summary>
    ///     Verifies <see cref="ConfigurationVersion.ToString" /> emits <c>Major.Minor</c>.
    /// </summary>
    [Test]
    public async Task ToString_AnyVersion_FormatsAsMajorMinor()
    {
        var version = new ConfigurationVersion(2, 10);

        await Assert.That(version.ToString()).IsEqualTo("2.10");
    }

    /// <summary>
    ///     Verifies major ordering dominates minor ordering in <see cref="ConfigurationVersion.CompareTo" />.
    /// </summary>
    [Test]
    public async Task CompareTo_DifferentMajor_OrdersByMajor()
    {
        var smaller = new ConfigurationVersion(1, 99);
        var bigger = new ConfigurationVersion(2, 0);

        await Assert.That(smaller.CompareTo(bigger)).IsLessThan(0);
        await Assert.That(bigger.CompareTo(smaller)).IsGreaterThan(0);
    }

    /// <summary>
    ///     Verifies minor is used as a tiebreaker when major versions are equal.
    /// </summary>
    [Test]
    public async Task CompareTo_SameMajor_OrdersByMinor()
    {
        var smaller = new ConfigurationVersion(1, 2);
        var bigger = new ConfigurationVersion(1, 5);

        await Assert.That(smaller.CompareTo(bigger)).IsLessThan(0);
        await Assert.That(bigger.CompareTo(smaller)).IsGreaterThan(0);
    }

    /// <summary>
    ///     Verifies <see cref="ConfigurationVersion.CompareTo" /> returns zero for equal versions.
    /// </summary>
    [Test]
    public async Task CompareTo_EqualVersions_ReturnsZero()
    {
        var a = new ConfigurationVersion(2, 3);
        var b = new ConfigurationVersion(2, 3);

        await Assert.That(a.CompareTo(b)).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies the <c>&lt;</c> operator returns true only for strictly smaller versions.
    /// </summary>
    [Test]
    public async Task OperatorLessThan_SmallerLeft_ReturnsTrue()
    {
        var a = new ConfigurationVersion(1, 0);
        var b = new ConfigurationVersion(1, 1);

        await Assert.That(a < b).IsTrue();
        await Assert.That(b < a).IsFalse();
        await Assert.That(a < a).IsFalse();
    }

    /// <summary>
    ///     Verifies the <c>&lt;=</c> operator returns true for smaller and equal versions.
    /// </summary>
    [Test]
    public async Task OperatorLessThanOrEqual_SmallerOrEqualLeft_ReturnsTrue()
    {
        var a = new ConfigurationVersion(1, 0);
        var b = new ConfigurationVersion(1, 1);

        await Assert.That(a <= b).IsTrue();
        await Assert.That(a <= a).IsTrue();
        await Assert.That(b <= a).IsFalse();
    }

    /// <summary>
    ///     Verifies the <c>&gt;</c> operator returns true only for strictly larger versions.
    /// </summary>
    [Test]
    public async Task OperatorGreaterThan_BiggerLeft_ReturnsTrue()
    {
        var a = new ConfigurationVersion(2, 0);
        var b = new ConfigurationVersion(1, 9);

        await Assert.That(a > b).IsTrue();
        await Assert.That(b > a).IsFalse();
        await Assert.That(a > a).IsFalse();
    }

    /// <summary>
    ///     Verifies the <c>&gt;=</c> operator returns true for larger and equal versions.
    /// </summary>
    [Test]
    public async Task OperatorGreaterThanOrEqual_BiggerOrEqualLeft_ReturnsTrue()
    {
        var a = new ConfigurationVersion(2, 0);
        var b = new ConfigurationVersion(1, 9);

        await Assert.That(a >= b).IsTrue();
        await Assert.That(a >= a).IsTrue();
        await Assert.That(b >= a).IsFalse();
    }

    /// <summary>
    ///     Verifies <see cref="ConfigurationVersion.HasLowerOrderThan" /> returns true only when strictly smaller.
    /// </summary>
    [Test]
    public async Task HasLowerOrderThan_SmallerLeft_ReturnsTrue()
    {
        var a = new ConfigurationVersion(1, 0);
        var b = new ConfigurationVersion(1, 1);

        await Assert.That(a.HasLowerOrderThan(b)).IsTrue();
        await Assert.That(a.HasLowerOrderThan(a)).IsFalse();
        await Assert.That(b.HasLowerOrderThan(a)).IsFalse();
    }
}
