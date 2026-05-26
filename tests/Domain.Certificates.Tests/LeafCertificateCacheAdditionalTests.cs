using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Tests for additional <see cref="LeafCertificateCache" /> behaviors.
/// </summary>
public sealed class LeafCertificateCacheAdditionalTests
{
    /// <summary>
    ///     Verifies that the constructor rejects a non-positive capacity.
    /// </summary>
    [Test]
    public async Task Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        await Assert.That(() => _ = new LeafCertificateCache(0)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that the constructor rejects a negative capacity.
    /// </summary>
    [Test]
    public async Task Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        await Assert.That(() => _ = new LeafCertificateCache(-1)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that the constructor stores the supplied capacity.
    /// </summary>
    [Test]
    public async Task Constructor_WithPositiveCapacity_StoresCapacity()
    {
        var cache = new LeafCertificateCache(42);

        await Assert.That(cache.Capacity).IsEqualTo(42);
        await Assert.That(cache.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="LeafCertificateCache.Evict" /> rejects a null host name.
    /// </summary>
    [Test]
    public async Task Evict_WithNullHostname_ThrowsArgumentException()
    {
        var cache = new LeafCertificateCache(2);

        await Assert.That(() => cache.Evict(null!)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="LeafCertificateCache.Evict" /> rejects a whitespace host name.
    /// </summary>
    [Test]
    public async Task Evict_WithWhitespaceHostname_ThrowsArgumentException()
    {
        var cache = new LeafCertificateCache(2);

        await Assert.That(() => cache.Evict("   ")).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="LeafCertificateCache.Evict" /> on a missing host name is a no-op.
    /// </summary>
    [Test]
    public async Task Evict_WithMissingHostname_DoesNothing()
    {
        var cache = new LeafCertificateCache(2);

        cache.Evict("not-present.example");

        await Assert.That(cache.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="LeafCertificateCache.Evict" /> on a cached host removes it.
    /// </summary>
    [Test]
    public async Task Evict_WithCachedHostname_RemovesEntry()
    {
        var cache = new LeafCertificateCache(2);
        var certificate = CertificateTestFactory.Create("host.example");
        cache.GetOrAdd("host.example", _ => certificate);

        cache.Evict("host.example");

        await Assert.That(cache.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="LeafCertificateCache.Clear" /> removes every cached entry.
    /// </summary>
    [Test]
    public async Task Clear_AfterEntriesAdded_RemovesAllEntries()
    {
        var cache = new LeafCertificateCache(4);
        cache.GetOrAdd("first.example", _ => CertificateTestFactory.Create("first.example"));
        cache.GetOrAdd("second.example", _ => CertificateTestFactory.Create("second.example"));

        cache.Clear();

        await Assert.That(cache.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="LeafCertificateCache.GetOrAdd" /> rejects a null host name.
    /// </summary>
    [Test]
    public async Task GetOrAdd_WithNullHostname_ThrowsArgumentException()
    {
        var cache = new LeafCertificateCache(2);

        await Assert.That(() => cache.GetOrAdd(null!, _ => CertificateTestFactory.Create("unused")))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="LeafCertificateCache.GetOrAdd" /> rejects a whitespace host name.
    /// </summary>
    [Test]
    public async Task GetOrAdd_WithWhitespaceHostname_ThrowsArgumentException()
    {
        var cache = new LeafCertificateCache(2);

        await Assert.That(() => cache.GetOrAdd("   ", _ => CertificateTestFactory.Create("unused")))
            .Throws<ArgumentException>();
    }
}