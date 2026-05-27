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
}
