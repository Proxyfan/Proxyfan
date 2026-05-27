using Proxyfan.Domain.Certificates;
using Proxyfan.Domain.Certificates.Tests.Stubs;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Tests for <see cref="MutableCertificateAuthorityProvider" />.
/// </summary>
public sealed class MutableCertificateAuthorityProviderTests
{
    /// <summary>
    ///     Verifies that <see cref="MutableCertificateAuthorityProvider.GetAsync" /> generates
    ///     the authority lazily on first call and returns the same task on subsequent calls.
    /// </summary>
    [Test]
    public async Task GetAsync_CalledTwice_ReturnsSameAuthority()
    {
        var generator = new CountingCertificateGenerator();
        var provider = new MutableCertificateAuthorityProvider(generator);

        var first = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);
        var second = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(first).IsSameReferenceAs(second);
        await Assert.That(generator.RootGenerationCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that <see cref="MutableCertificateAuthorityProvider.RegenerateAsync" /> replaces
    ///     the current authority with a fresh instance.
    /// </summary>
    [Test]
    public async Task RegenerateAsync_AfterGet_ReplacesAuthority()
    {
        var generator = new CountingCertificateGenerator();
        var provider = new MutableCertificateAuthorityProvider(generator);
        var original = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);

        var rotated = await provider.RegenerateAsync(CancellationToken.None).ConfigureAwait(false);
        var current = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(rotated).IsNotSameReferenceAs(original);
        await Assert.That(current).IsSameReferenceAs(rotated);
        await Assert.That(generator.RootGenerationCount).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that <see cref="MutableCertificateAuthorityProvider.RegenerateAsync" /> raises
    ///     the <see cref="MutableCertificateAuthorityProvider.Changed" /> event exactly once.
    /// </summary>
    [Test]
    public async Task RegenerateAsync_WhenInvoked_RaisesChangedEventOnce()
    {
        var generator = new CountingCertificateGenerator();
        var provider = new MutableCertificateAuthorityProvider(generator);
        var changedCount = 0;
        provider.Changed += _ => Interlocked.Increment(ref changedCount);

        await provider.RegenerateAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(changedCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that <see cref="MutableCertificateAuthorityProvider.RegenerateAsync" /> can be
    ///     called repeatedly to rotate the held authority each time.
    /// </summary>
    [Test]
    public async Task RegenerateAsync_CalledTwice_GeneratesThreeAuthorities()
    {
        var generator = new CountingCertificateGenerator();
        var provider = new MutableCertificateAuthorityProvider(generator);
        await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);

        await provider.RegenerateAsync(CancellationToken.None).ConfigureAwait(false);
        await provider.RegenerateAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(generator.RootGenerationCount).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that <see cref="MutableCertificateAuthorityProvider.GetAsync" /> after a
    ///     regeneration returns the regenerated authority rather than the original.
    /// </summary>
    [Test]
    public async Task GetAsync_AfterRegenerate_ReturnsNewAuthority()
    {
        var generator = new CountingCertificateGenerator();
        var provider = new MutableCertificateAuthorityProvider(generator);
        var original = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);
        await provider.RegenerateAsync(CancellationToken.None).ConfigureAwait(false);

        var current = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(current).IsNotSameReferenceAs(original);
    }
}
