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
}
