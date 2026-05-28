using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Tests for <see cref="ServerNameIndicationProxyingList" />.
/// </summary>
public sealed class ServerNameIndicationProxyingListTests
{
    /// <summary>
    ///     Verifies that disabled proxying never matches.
    /// </summary>
    [Test]
    public async Task HasMatch_WhenDisabled_ReturnsFalse()
    {
        var list = new ServerNameIndicationProxyingList(false);
        list.AddIncludedPattern("example.com");

        var result = list.HasMatch("example.com");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that excluded patterns take precedence over included patterns.
    /// </summary>
    [Test]
    public async Task HasMatch_WhenExcludedPatternMatches_ReturnsFalse()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("*.example.com");
        list.AddExcludedPattern("blocked.example.com");

        var result = list.HasMatch("blocked.example.com");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that included wildcard patterns match subdomains.
    /// </summary>
    [Test]
    public async Task HasMatch_WhenIncludedWildcardMatches_ReturnsTrue()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("*.example.com");

        var result = list.HasMatch("api.example.com");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that an excluded pattern that does NOT match is skipped and an included
    ///     wildcard pattern then matches the hostname (covers the fall-through of the
    ///     excluded loop without an early <c>return false</c>).
    /// </summary>
    [Test]
    public async Task HasMatch_ExcludedPatternDoesNotMatchButIncludedDoes_ReturnsTrue()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddExcludedPattern("blocked.example.com");
        list.AddIncludedPattern("*.example.com");

        var result = list.HasMatch("api.example.com");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that a non-wildcard included pattern that does not equal the hostname
    ///     returns false (covers the <c>!pattern.StartsWith("*.")</c> branch of the wildcard
    ///     matcher).
    /// </summary>
    [Test]
    public async Task HasMatch_NonWildcardIncludedPatternDoesNotEqualHostname_ReturnsFalse()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("exact.example.com");

        var result = list.HasMatch("different.example.com");

        await Assert.That(result).IsFalse();
    }
}