using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Branch coverage tests for <see cref="ServerNameIndicationProxyingList" /> targeting
///     the null/empty hostname and exact-suffix-match edge cases.
/// </summary>
public sealed class ServerNameIndicationProxyingListBranchTests
{
    /// <summary>
    ///     Verifies that a null host name returns false even when enabled.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task HasMatch_NullOrEmptyHostname_ReturnsFalse(string? hostname)
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("*");

        await Assert.That(list.HasMatch(hostname!)).IsFalse();
    }

    /// <summary>
    ///     Verifies that a host equal to the wildcard suffix (e.g. host="example.com" pattern="*.example.com")
    ///     does NOT match (only true subdomains do).
    /// </summary>
    [Test]
    public async Task HasMatch_HostEqualToWildcardSuffix_ReturnsFalse()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("*.example.com");

        var matched = list.HasMatch("example.com");

        await Assert.That(matched).IsFalse();
    }
}
