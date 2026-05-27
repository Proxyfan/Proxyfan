using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Updates;

namespace Proxyfan.Domain.Updates.Tests;

/// <summary>
///     Tests for <see cref="SemanticVersionComparer" />.
/// </summary>
public sealed class SemanticVersionComparerTests
{
    /// <summary>
    ///     Verifies IsNewer returns true when patch is greater.
    /// </summary>
    [Test]
    [Arguments("1.0.0", "1.0.1", true)]
    [Arguments("1.0.0", "1.1.0", true)]
    [Arguments("1.0.0", "2.0.0", true)]
    [Arguments("1.2.3", "1.2.3", false)]
    [Arguments("1.2.4", "1.2.3", false)]
    [Arguments("2.0.0", "1.99.99", false)]
    public async Task IsNewer_Variants_ReturnsExpected(string current, string candidate, bool expected)
    {
        var result = SemanticVersionComparer.HasNewerVersion(current, candidate);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies pre-release suffix is ignored.
    /// </summary>
    [Test]
    public async Task IsNewer_PreReleaseSuffix_IgnoresMetadata()
    {
        var result = SemanticVersionComparer.HasNewerVersion("1.0.0", "1.0.1-alpha");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies build metadata is ignored.
    /// </summary>
    [Test]
    public async Task IsNewer_BuildMetadata_IgnoresMetadata()
    {
        var result = SemanticVersionComparer.HasNewerVersion("1.0.0", "1.0.0+build.123");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies invalid versions throw.
    /// </summary>
    [Test]
    [Arguments("1.0", "1.0.1")]
    [Arguments("1.0.0", "not-a-version")]
    [Arguments("abc.def.ghi", "1.0.0")]
    public async Task IsNewer_InvalidVersion_Throws(string current, string candidate)
    {
        await Assert.That(() => SemanticVersionComparer.HasNewerVersion(current, candidate)).Throws<ArgumentException>();
    }
}
