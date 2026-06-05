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
        await Assert.That(certificate.Handle).IsEqualTo(IntPtr.Zero);
    }

    /// <summary>
    ///     Verifies that evicting via a differently-cased host name removes the canonical cache entry.
    /// </summary>
    [Test]
    public async Task Evict_HostnameDiffersByCase_RemovesCanonicalEntry()
    {
        var cache = new LeafCertificateCache(2);
        var certificate = CertificateTestFactory.Create("example.com");
        cache.GetOrAdd("example.com", _ => certificate);

        cache.Evict("Example.COM");

        await Assert.That(cache.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="LeafCertificateCache.Clear" /> removes every cached entry.
    /// </summary>
    [Test]
    public async Task Clear_AfterEntriesAdded_RemovesAllEntries()
    {
        var cache = new LeafCertificateCache(4);
        var firstCertificate = CertificateTestFactory.Create("first.example");
        var secondCertificate = CertificateTestFactory.Create("second.example");
        cache.GetOrAdd("first.example", _ => firstCertificate);
        cache.GetOrAdd("second.example", _ => secondCertificate);

        cache.Clear();

        await Assert.That(cache.Count).IsEqualTo(0);
        await Assert.That(firstCertificate.Handle).IsEqualTo(IntPtr.Zero);
        await Assert.That(secondCertificate.Handle).IsEqualTo(IntPtr.Zero);
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

    /// <summary>
    ///     Verifies that the cache treats host names that differ only in case as the same entry.
    /// </summary>
    [Test]
    public async Task GetOrAdd_HostnameDiffersByCase_ReusesCertificate()
    {
        var cache = new LeafCertificateCache(2);
        var certificate = CertificateTestFactory.Create("example.com");

        var first = cache.GetOrAdd("example.com", _ => certificate);
        var second = cache.GetOrAdd("Example.COM", _ => CertificateTestFactory.Create("Example.COM"));

        await Assert.That(second).IsSameReferenceAs(first);
        await Assert.That(cache.Count).IsEqualTo(1);
    }
}