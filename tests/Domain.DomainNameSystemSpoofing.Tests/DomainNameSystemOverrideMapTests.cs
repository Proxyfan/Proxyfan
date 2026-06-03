using System.Net;
using System.Threading.Tasks;

namespace Proxyfan.Domain.DomainNameSystemSpoofing.Tests;

/// <summary>
///     Tests for <see cref="DomainNameSystemOverrideMap" />.
/// </summary>
public sealed class DomainNameSystemOverrideMapTests
{
    /// <summary>
    ///     Verifies that an empty map has count zero.
    /// </summary>
    [Test]
    public async Task Count_Empty_IsZero()
    {
        var map = new DomainNameSystemOverrideMap();

        await Assert.That(map.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that Add then HasOverride returns true.
    /// </summary>
    [Test]
    public async Task HasOverride_AfterAdd_ReturnsTrue()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));

        await Assert.That(map.HasOverride("api.example.com")).IsTrue();
    }

    /// <summary>
    ///     Verifies that HasOverride lookup is case-insensitive.
    /// </summary>
    [Test]
    public async Task HasOverride_DifferentCase_ReturnsTrue()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));

        await Assert.That(map.HasOverride("API.EXAMPLE.COM")).IsTrue();
    }

    /// <summary>
    ///     Verifies that HasOverride returns false for an unknown hostname.
    /// </summary>
    [Test]
    public async Task HasOverride_UnknownHostname_ReturnsFalse()
    {
        var map = new DomainNameSystemOverrideMap();

        await Assert.That(map.HasOverride("unknown.local")).IsFalse();
    }

    /// <summary>
    ///     Verifies that Resolve returns the configured address.
    /// </summary>
    [Test]
    public async Task Resolve_ConfiguredHostname_ReturnsAddress()
    {
        var map = new DomainNameSystemOverrideMap();
        var address = IPAddress.Parse("10.0.0.1");
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", address));

        var resolved = map.Resolve("api.example.com");

        await Assert.That(resolved).IsSameReferenceAs(address);
    }

    /// <summary>
    ///     Verifies that Resolve on an unknown hostname returns null.
    /// </summary>
    [Test]
    public async Task Resolve_UnknownHostname_ReturnsNull()
    {
        var map = new DomainNameSystemOverrideMap();

        var resolved = map.Resolve("unknown.local");

        await Assert.That(resolved).IsNull();
    }

    /// <summary>
    ///     Verifies that HasRemoved removes the configured entry.
    /// </summary>
    [Test]
    public async Task HasRemoved_ConfiguredHostname_RemovesEntry()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));

        var removed = map.HasRemoved("api.example.com");

        await Assert.That(removed).IsTrue();
        await Assert.That(map.HasOverride("api.example.com")).IsFalse();
        await Assert.That(map.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that HasRemoved on an unknown hostname returns false.
    /// </summary>
    [Test]
    public async Task HasRemoved_UnknownHostname_ReturnsFalse()
    {
        var map = new DomainNameSystemOverrideMap();

        var removed = map.HasRemoved("unknown.local");

        await Assert.That(removed).IsFalse();
    }

    /// <summary>
    ///     Verifies that Add for an existing hostname replaces the existing override.
    /// </summary>
    [Test]
    public async Task Add_TwiceForSameHostname_ReplacesAddress()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Parse("10.0.0.1")));
        var newAddress = IPAddress.Parse("10.0.0.2");
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", newAddress));

        var resolved = map.Resolve("api.example.com");

        await Assert.That(resolved).IsSameReferenceAs(newAddress);
        await Assert.That(map.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that newly created maps default to active.
    /// </summary>
    [Test]
    public async Task IsActive_OnNewMap_DefaultsToTrue()
    {
        var map = new DomainNameSystemOverrideMap();

        await Assert.That(map.IsActive).IsTrue();
    }

    /// <summary>
    ///     Verifies that disabling the master toggle stops resolution but preserves entries.
    /// </summary>
    [Test]
    public async Task Resolve_WhenInactive_ReturnsNullAndPreservesEntries()
    {
        var map = new DomainNameSystemOverrideMap();
        var address = IPAddress.Parse("10.0.0.1");
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", address));
        map.IsActive = false;

        var resolved = map.Resolve("api.example.com");

        await Assert.That(resolved).IsNull();
        await Assert.That(map.HasOverride("api.example.com")).IsTrue();
    }

    /// <summary>
    ///     Verifies that re-enabling the master toggle resumes resolution.
    /// </summary>
    [Test]
    public async Task Resolve_AfterReactivating_ReturnsAddress()
    {
        var map = new DomainNameSystemOverrideMap();
        var address = IPAddress.Parse("10.0.0.1");
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", address));
        map.IsActive = false;
        map.IsActive = true;

        var resolved = map.Resolve("api.example.com");

        await Assert.That(resolved).IsSameReferenceAs(address);
    }

    /// <summary>
    ///     Verifies that disabled entries are skipped during resolution.
    /// </summary>
    [Test]
    public async Task Resolve_WhenEntryDisabled_ReturnsNull()
    {
        var map = new DomainNameSystemOverrideMap();
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);
        entry.IsEnabled = false;
        map.Add(entry);

        var resolved = map.Resolve("api.example.com");

        await Assert.That(resolved).IsNull();
    }

    /// <summary>
    ///     Verifies that match counter is incremented on every successful resolution.
    /// </summary>
    [Test]
    public async Task Resolve_OnMatch_IncrementsEntryMatchCount()
    {
        var map = new DomainNameSystemOverrideMap();
        var entry = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback);
        map.Add(entry);

        map.Resolve("api.example.com");
        map.Resolve("api.example.com");

        await Assert.That(entry.MatchCount).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies a wildcard pattern matches a sub-domain.
    /// </summary>
    [Test]
    public async Task Resolve_WildcardPatternMatchingSubdomain_ReturnsAddress()
    {
        var map = new DomainNameSystemOverrideMap();
        var address = IPAddress.Parse("10.0.0.5");
        map.Add(new DomainNameSystemOverrideEntry("*.example.com", address));

        var resolved = map.Resolve("api.example.com");

        await Assert.That(resolved).IsSameReferenceAs(address);
    }

    /// <summary>
    ///     Verifies the wildcard does NOT match the apex.
    /// </summary>
    [Test]
    public async Task Resolve_WildcardPatternAgainstApex_ReturnsNull()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("*.example.com", IPAddress.Loopback));

        var resolved = map.Resolve("example.com");

        await Assert.That(resolved).IsNull();
    }

    /// <summary>
    ///     Verifies an exact entry wins over an overlapping wildcard.
    /// </summary>
    [Test]
    public async Task Resolve_ExactAndOverlappingWildcard_ExactWins()
    {
        var map = new DomainNameSystemOverrideMap();
        var exactAddress = IPAddress.Parse("10.0.0.1");
        var wildcardAddress = IPAddress.Parse("10.0.0.2");
        map.Add(new DomainNameSystemOverrideEntry("*.example.com", wildcardAddress));
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", exactAddress));

        var resolved = map.Resolve("api.example.com");

        await Assert.That(resolved).IsSameReferenceAs(exactAddress);
    }

    /// <summary>
    ///     Verifies the longest enabled wildcard suffix wins when several wildcards overlap.
    /// </summary>
    [Test]
    public async Task Resolve_MultipleOverlappingWildcards_LongestSuffixWins()
    {
        var map = new DomainNameSystemOverrideMap();
        var shortAddress = IPAddress.Parse("10.0.0.1");
        var longAddress = IPAddress.Parse("10.0.0.2");
        map.Add(new DomainNameSystemOverrideEntry("*.com", shortAddress));
        map.Add(new DomainNameSystemOverrideEntry("*.example.com", longAddress));

        var resolved = map.Resolve("api.example.com");

        await Assert.That(resolved).IsSameReferenceAs(longAddress);
    }

    /// <summary>
    ///     Verifies that a disabled exact entry falls through to an enabled wildcard.
    /// </summary>
    [Test]
    public async Task Resolve_DisabledExactAndEnabledWildcard_FallsThroughToWildcard()
    {
        var map = new DomainNameSystemOverrideMap();
        var exact = new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Parse("10.0.0.1"));
        exact.IsEnabled = false;
        var wildcardAddress = IPAddress.Parse("10.0.0.2");
        var wildcard = new DomainNameSystemOverrideEntry("*.example.com", wildcardAddress);
        map.Add(exact);
        map.Add(wildcard);

        var resolved = map.Resolve("api.example.com");

        await Assert.That(resolved).IsSameReferenceAs(wildcardAddress);
    }

    /// <summary>
    ///     Verifies GetSnapshot returns all entries (enabled and disabled) in insertion order.
    /// </summary>
    [Test]
    public async Task GetSnapshot_WithMultipleEntries_ReturnsAllEntries()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));
        map.Add(new DomainNameSystemOverrideEntry("*.other.com", IPAddress.Loopback));

        var snapshot = map.GetSnapshot();

        await Assert.That(snapshot).HasCount(2);
        await Assert.That(snapshot[0].Hostname).IsEqualTo("api.example.com");
        await Assert.That(snapshot[1].Hostname).IsEqualTo("*.other.com");
    }

    /// <summary>
    ///     Verifies HasOverride matches by canonical pattern (case-insensitive, dot-stripped).
    /// </summary>
    [Test]
    public async Task HasOverride_TrailingDotAndMixedCase_MatchesCanonical()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));

        await Assert.That(map.HasOverride("API.Example.COM.")).IsTrue();
    }

    /// <summary>
    ///     Verifies HasSetEnabled flips the underlying entry and reports success.
    /// </summary>
    [Test]
    public async Task HasSetEnabled_WithMatchingEntry_UpdatesEntryAndReturnsTrue()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));

        var result = map.HasSetEnabled("api.example.com", isEnabled: false);

        await Assert.That(result).IsTrue();
        await Assert.That(map.GetSnapshot()[0].IsEnabled).IsFalse();
        await Assert.That(map.Resolve("api.example.com")).IsNull();
    }

    /// <summary>
    ///     Verifies HasSetEnabled returns false when no entry matches.
    /// </summary>
    [Test]
    public async Task HasSetEnabled_WithUnknownHostname_ReturnsFalse()
    {
        var map = new DomainNameSystemOverrideMap();

        var result = map.HasSetEnabled("missing.example.com", isEnabled: false);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies HasResetMatchCount zeroes the entry's counter and reports success.
    /// </summary>
    [Test]
    public async Task HasResetMatchCount_WithMatchingEntry_ZeroesCounterAndReturnsTrue()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));
        map.Resolve("api.example.com");
        map.Resolve("api.example.com");

        var result = map.HasResetMatchCount("api.example.com");

        await Assert.That(result).IsTrue();
        await Assert.That(map.GetSnapshot()[0].MatchCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies HasResetMatchCount returns false when no entry matches.
    /// </summary>
    [Test]
    public async Task HasResetMatchCount_WithUnknownHostname_ReturnsFalse()
    {
        var map = new DomainNameSystemOverrideMap();

        var result = map.HasResetMatchCount("missing.example.com");

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies GetMatchCount reads the entry's current counter.
    /// </summary>
    [Test]
    public async Task GetMatchCount_AfterResolves_ReturnsCurrentCount()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Loopback));
        map.Resolve("api.example.com");
        map.Resolve("api.example.com");
        map.Resolve("api.example.com");

        await Assert.That(map.GetMatchCount("api.example.com")).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies GetMatchCount returns null when no entry matches.
    /// </summary>
    [Test]
    public async Task GetMatchCount_WithUnknownHostname_ReturnsNull()
    {
        var map = new DomainNameSystemOverrideMap();

        await Assert.That(map.GetMatchCount("missing.example.com")).IsNull();
    }
}
