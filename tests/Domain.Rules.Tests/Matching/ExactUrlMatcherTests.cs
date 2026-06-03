using Proxyfan.Domain.Rules.Matching;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Matching;

/// <summary>
///     Tests for <see cref="ExactUrlMatcher" />.
/// </summary>
public sealed class ExactUrlMatcherTests
{
    /// <summary>
    ///     Verifies the constructor rejects a null pattern.
    /// </summary>
    [Test]
    public async Task Constructor_WithNullPattern_Throws()
    {
        await Assert.That(() => _ = new ExactUrlMatcher(null!)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies the constructor rejects an empty pattern.
    /// </summary>
    [Test]
    public async Task Constructor_WithEmptyPattern_Throws()
    {
        await Assert.That(() => _ = new ExactUrlMatcher(string.Empty)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that exact pattern matches return true (case-insensitive).
    /// </summary>
    [Test]
    public async Task Matches_ExactPatternIgnoringCase_ReturnsTrue()
    {
        var matcher = new ExactUrlMatcher("https://Example.com/");

        var result = matcher.HasMatch("https://example.com/");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that non-matching URLs return false.
    /// </summary>
    [Test]
    public async Task Matches_DifferentUrl_ReturnsFalse()
    {
        var matcher = new ExactUrlMatcher("https://example.com/");

        var result = matcher.HasMatch("https://other.example.com/");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that an empty URL is rejected.
    /// </summary>
    [Test]
    public async Task Matches_EmptyUrl_ReturnsFalse()
    {
        var matcher = new ExactUrlMatcher("https://example.com/");

        var result = matcher.HasMatch(string.Empty);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that paths differing only in case are not considered equal.
    /// </summary>
    [Test]
    public async Task Matches_PathDifferingByCase_ReturnsFalse()
    {
        var matcher = new ExactUrlMatcher("https://example.com/Path");

        var result = matcher.HasMatch("https://example.com/path");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that queries differing only in case are not considered equal.
    /// </summary>
    [Test]
    public async Task Matches_QueryDifferingByCase_ReturnsFalse()
    {
        var matcher = new ExactUrlMatcher("https://example.com/path?Token=ABC");

        var result = matcher.HasMatch("https://example.com/path?Token=abc");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that the scheme is compared case-insensitively.
    /// </summary>
    [Test]
    public async Task Matches_SchemeDifferingByCase_ReturnsTrue()
    {
        var matcher = new ExactUrlMatcher("HTTPS://example.com/path");

        var result = matcher.HasMatch("https://example.com/path");

        await Assert.That(result).IsTrue();
    }
}
