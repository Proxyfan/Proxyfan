using System;
using System.Net;
using System.Threading.Tasks;

namespace Proxyfan.Domain.DomainNameSystemSpoofing.Tests;

/// <summary>
///     Tests for <see cref="DomainNameSystemOverrideEntry" />.
/// </summary>
public sealed class DomainNameSystemOverrideEntryTests
{
    /// <summary>
    ///     Verifies that the constructor stores the supplied hostname and address.
    /// </summary>
    [Test]
    public async Task Constructor_GivenHostnameAndAddress_StoresBoth()
    {
        var address = IPAddress.Parse("127.0.0.1");

        var entry = new DomainNameSystemOverrideEntry("example.com", address);

        await Assert.That(entry.Hostname).IsEqualTo("example.com");
        await Assert.That(entry.OverrideAddress).IsSameReferenceAs(address);
    }

    /// <summary>
    ///     Verifies that a null or blank hostname throws.
    /// </summary>
    /// <param name="hostname">The hostname to test.</param>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Constructor_BlankHostname_Throws(string? hostname)
    {
        await Assert.That(() => new DomainNameSystemOverrideEntry(hostname!, IPAddress.Loopback))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that hostnames containing inner whitespace are rejected.
    /// </summary>
    [Test]
    public async Task Constructor_HostnameWithInnerSpace_Throws()
    {
        await Assert.That(() => new DomainNameSystemOverrideEntry("ex ample.com", IPAddress.Loopback))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that a bare wildcard prefix (no domain suffix) is rejected.
    /// </summary>
    [Test]
    public async Task Constructor_BareWildcardPrefix_Throws()
    {
        await Assert.That(() => new DomainNameSystemOverrideEntry("*.", IPAddress.Loopback))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies the constructor defaults the entry to enabled with a zero match count.
    /// </summary>
    [Test]
    public async Task Constructor_NewEntry_DefaultsToEnabledWithZeroMatchCount()
    {
        var entry = new DomainNameSystemOverrideEntry("example.com", IPAddress.Loopback);

        await Assert.That(entry.IsEnabled).IsTrue();
        await Assert.That(entry.MatchCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies exact patterns expose the correct Kind and empty wildcard suffix.
    /// </summary>
    [Test]
    public async Task Constructor_ExactPattern_KindIsExact()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);

        await Assert.That(entry.Kind).IsEqualTo(DomainOverrideKind.Exact);
        await Assert.That(entry.WildcardSuffix).IsEqualTo(string.Empty);
        await Assert.That(entry.CanonicalPattern).IsEqualTo("api.example.com");
    }

    /// <summary>
    ///     Verifies wildcard patterns expose the wildcard suffix and lower-cased canonical form.
    /// </summary>
    [Test]
    public async Task Constructor_WildcardPattern_KindIsWildcard()
    {
        var entry = new DomainNameSystemOverrideEntry("*.Example.COM", IPAddress.Loopback);

        await Assert.That(entry.Kind).IsEqualTo(DomainOverrideKind.WildcardSuffix);
        await Assert.That(entry.WildcardSuffix).IsEqualTo(".example.com");
        await Assert.That(entry.CanonicalPattern).IsEqualTo("*.example.com");
    }

    /// <summary>
    ///     Verifies the canonical form normalises trailing dots and case.
    /// </summary>
    [Test]
    public async Task Constructor_TrailingDotAndUppercase_NormalisesCanonical()
    {
        var entry = new DomainNameSystemOverrideEntry("API.Example.COM.", IPAddress.Loopback);

        await Assert.That(entry.CanonicalPattern).IsEqualTo("api.example.com");
        await Assert.That(entry.Hostname).IsEqualTo("API.Example.COM.");
    }

    /// <summary>
    ///     Verifies HasMatch returns true for the canonical form of the exact pattern.
    /// </summary>
    [Test]
    public async Task HasMatch_ExactPatternMatchingCanonical_ReturnsTrue()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);

        await Assert.That(entry.HasMatch("api.example.com")).IsTrue();
    }

    /// <summary>
    ///     Verifies HasMatch returns false for a different exact host name.
    /// </summary>
    [Test]
    public async Task HasMatch_ExactPatternDifferentHost_ReturnsFalse()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);

        await Assert.That(entry.HasMatch("other.example.com")).IsFalse();
    }

    /// <summary>
    ///     Verifies a wildcard pattern matches any sub-domain of the suffix.
    /// </summary>
    /// <param name="hostname">The hostname to test.</param>
    [Test]
    [Arguments("api.example.com")]
    [Arguments("deep.api.example.com")]
    public async Task HasMatch_WildcardPatternMatchingSubdomain_ReturnsTrue(string hostname)
    {
        var entry = new DomainNameSystemOverrideEntry("*.example.com", IPAddress.Loopback);

        await Assert.That(entry.HasMatch(hostname)).IsTrue();
    }

    /// <summary>
    ///     Verifies a wildcard pattern does NOT match the bare apex (per Charles/Fiddler parity).
    /// </summary>
    [Test]
    public async Task HasMatch_WildcardPatternAgainstApex_ReturnsFalse()
    {
        var entry = new DomainNameSystemOverrideEntry("*.example.com", IPAddress.Loopback);

        await Assert.That(entry.HasMatch("example.com")).IsFalse();
    }

    /// <summary>
    ///     Verifies a wildcard pattern does NOT match an unrelated host.
    /// </summary>
    [Test]
    public async Task HasMatch_WildcardPatternUnrelatedHost_ReturnsFalse()
    {
        var entry = new DomainNameSystemOverrideEntry("*.example.com", IPAddress.Loopback);

        await Assert.That(entry.HasMatch("api.other.com")).IsFalse();
    }

    /// <summary>
    ///     Verifies HasMatch throws on null or whitespace input.
    /// </summary>
    /// <param name="hostname">The hostname to test.</param>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task HasMatch_BlankHostname_Throws(string? hostname)
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);

        await Assert.That(() => entry.HasMatch(hostname!)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies IsEnabled is a round-trip property.
    /// </summary>
    [Test]
    public async Task IsEnabled_SetFalseAndTrue_RoundTrips()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);

        entry.IsEnabled = false;
        await Assert.That(entry.IsEnabled).IsFalse();
        entry.IsEnabled = true;
        await Assert.That(entry.IsEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies RecordMatch increments the match counter and returns the new value.
    /// </summary>
    [Test]
    public async Task RecordMatch_CalledTwice_IncrementsCounterAndReturnsNewValue()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);

        var first = entry.RecordMatch();
        var second = entry.RecordMatch();

        await Assert.That(first).IsEqualTo(1);
        await Assert.That(second).IsEqualTo(2);
        await Assert.That(entry.MatchCount).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies ResetMatchCount returns the counter to zero.
    /// </summary>
    [Test]
    public async Task ResetMatchCount_AfterRecordMatch_ReturnsToZero()
    {
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);
        entry.RecordMatch();
        entry.RecordMatch();

        entry.ResetMatchCount();

        await Assert.That(entry.MatchCount).IsEqualTo(0);
    }
}
