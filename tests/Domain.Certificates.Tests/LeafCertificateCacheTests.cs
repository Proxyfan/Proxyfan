using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Tests for <see cref="LeafCertificateCache" />.
/// </summary>
public sealed class LeafCertificateCacheTests
{
    /// <summary>
    ///     Verifies that the cache returns the existing certificate and updates least-recently-used ordering.
    /// </summary>
    [Test]
    public async Task GetOrAdd_WhenEntryIsReused_PreservesRecentlyAccessedCertificate()
    {
        var cache = new LeafCertificateCache(2);
        var firstCertificate = CreateCertificate("first.example");
        var secondCertificate = CreateCertificate("second.example");
        var thirdCertificate = CreateCertificate("third.example");
        cache.GetOrAdd("first.example", _ => firstCertificate);
        cache.GetOrAdd("second.example", _ => secondCertificate);

        var reusedCertificate = cache.GetOrAdd("first.example", _ => CreateCertificate("replacement.example"));
        cache.GetOrAdd("third.example", _ => thirdCertificate);

        await Assert.That(reusedCertificate).IsSameReferenceAs(firstCertificate);
        await Assert.That(cache.GetOrAdd("first.example", _ => CreateCertificate("unused.example"))).IsSameReferenceAs(firstCertificate);
        await Assert.That(cache.GetOrAdd("third.example", _ => CreateCertificate("unused-third.example"))).IsSameReferenceAs(thirdCertificate);
    }

    /// <summary>
    ///     Verifies that the cache evicts the oldest certificate when capacity is exceeded.
    /// </summary>
    [Test]
    public async Task GetOrAdd_WhenCapacityIsExceeded_EvictsLeastRecentlyUsedCertificate()
    {
        var cache = new LeafCertificateCache(2);
        var firstCertificate = CreateCertificate("first.example");
        var secondCertificate = CreateCertificate("second.example");
        var thirdCertificate = CreateCertificate("third.example");
        cache.GetOrAdd("first.example", _ => firstCertificate);
        cache.GetOrAdd("second.example", _ => secondCertificate);

        cache.GetOrAdd("third.example", _ => thirdCertificate);

        await Assert.That(cache.Count).IsEqualTo(2);
        await Assert.That(cache.GetOrAdd("second.example", _ => CreateCertificate("unused.example"))).IsSameReferenceAs(secondCertificate);
        await Assert.That(cache.GetOrAdd("third.example", _ => CreateCertificate("unused-third.example"))).IsSameReferenceAs(thirdCertificate);
    }

    /// <summary>
    ///     Verifies that concurrent access for the same host name creates only one certificate instance.
    /// </summary>
    [Test]
    public async Task GetOrAdd_WhenAccessedConcurrently_CreatesSingleCertificate()
    {
        var cache = new LeafCertificateCache(8);
        var factoryCallCount = 0;
        var tasks = new List<Task<X509Certificate2>>();

        for (var index = 0; index < 16; index++)
        {
            tasks.Add(Task.Run(() => cache.GetOrAdd("shared.example", hostName => CreateTrackedCertificate(hostName, ref factoryCallCount))));
        }

        var results = await Task.WhenAll(tasks);

        await Assert.That(factoryCallCount).IsEqualTo(1);
        await Assert.That(cache.Count).IsEqualTo(1);
        await Assert.That(results[0]).IsSameReferenceAs(results[^1]);
    }

    /// <summary>
    ///     Verifies that a cache hit for another host is not blocked while a new certificate is being created.
    /// </summary>
    [Test]
    public async Task GetOrAdd_WhenAnotherHostIsGenerating_DoesNotBlockCachedHit()
    {
        var cache = new LeafCertificateCache(8);
        var cachedCertificate = CreateCertificate("cached.example");
        var factoryCallCount = 0;
        using var factoryEntered = new ManualResetEventSlim();
        using var allowFactoryToContinue = new ManualResetEventSlim();
        cache.GetOrAdd("cached.example", _ => cachedCertificate);

        var generatingTask = Task.Run(() => cache.GetOrAdd("blocked.example", _ =>
        {
            Interlocked.Increment(ref factoryCallCount);
            factoryEntered.Set();
            allowFactoryToContinue.Wait(TimeSpan.FromSeconds(5));
            return CreateCertificate("blocked.example");
        }));

        await Assert.That(factoryEntered.Wait(TimeSpan.FromSeconds(5))).IsTrue();

        var stopwatch = Stopwatch.StartNew();
        var cachedHitTask = Task.Run(() => cache.GetOrAdd(
            "cached.example",
            _ => throw new InvalidOperationException("Factory should not be used for cached entries.")));
        var cachedHitCompleted = cachedHitTask.Wait(TimeSpan.FromSeconds(1));
        stopwatch.Stop();

        allowFactoryToContinue.Set();

        await Assert.That(cachedHitCompleted).IsTrue();
        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(1));
        await Assert.That(factoryCallCount).IsEqualTo(1);
        await Assert.That(cachedHitTask.Result).IsSameReferenceAs(cachedCertificate);
        await Assert.That(await generatingTask).IsNotNull();
    }

    private static X509Certificate2 CreateCertificate(string hostname)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={hostname}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var certificateBytes = certificate.Export(X509ContentType.Pfx);
        var cachedCertificate = X509CertificateLoader.LoadPkcs12(certificateBytes, string.Empty);
        return cachedCertificate;
    }

    private static X509Certificate2 CreateTrackedCertificate(string hostname, ref int factoryCallCount)
    {
        Interlocked.Increment(ref factoryCallCount);
        return CreateCertificate(hostname);
    }
}