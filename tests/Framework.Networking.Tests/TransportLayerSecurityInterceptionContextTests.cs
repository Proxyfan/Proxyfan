using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptionContext" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptionContextTests
{
    /// <summary>
    ///     Verifies that the proxying list passed to the constructor is returned unchanged.
    /// </summary>
    [Test]
    public async Task ProxyingList_Always_ReturnsSameInstanceAsConfigured()
    {
        var proxyingList = new ServerNameIndicationProxyingList(isEnabled: true);
        var context = new TransportLayerSecurityInterceptionContext(new MutableCertificateAuthorityProvider(new StubCertificateGenerator()), proxyingList);

        await Assert.That(context.ProxyingList).IsSameReferenceAs(proxyingList);
    }

    /// <summary>
    ///     Verifies that a leaf certificate is generated for a given hostname.
    /// </summary>
    [Test]
    public async Task GetLeafCertificateAsync_ValidHostname_ReturnsCertificateWithSubject()
    {
        var proxyingList = new ServerNameIndicationProxyingList(isEnabled: false);
        var context = new TransportLayerSecurityInterceptionContext(new MutableCertificateAuthorityProvider(new StubCertificateGenerator()), proxyingList);

        var certificate = await context.GetLeafCertificateAsync("example.com", CancellationToken.None);

        await Assert.That(certificate).IsNotNull();
        await Assert.That(certificate.SubjectName.Name).IsNotNull();
    }

    /// <summary>
    ///     Verifies that a leaf certificate is returned from cache on the second call for the same hostname.
    /// </summary>
    [Test]
    public async Task GetLeafCertificateAsync_SameHostnameTwice_ReturnsSameCertificate()
    {
        var proxyingList = new ServerNameIndicationProxyingList(isEnabled: false);
        var context = new TransportLayerSecurityInterceptionContext(new MutableCertificateAuthorityProvider(new StubCertificateGenerator()), proxyingList);

        var first = await context.GetLeafCertificateAsync("cached.example.com", CancellationToken.None);
        var second = await context.GetLeafCertificateAsync("cached.example.com", CancellationToken.None);

        await Assert.That(first).IsSameReferenceAs(second);
    }

    /// <summary>
    ///     Verifies that different hostnames produce different certificates.
    /// </summary>
    [Test]
    public async Task GetLeafCertificateAsync_DifferentHostnames_ReturnsDifferentCertificates()
    {
        var proxyingList = new ServerNameIndicationProxyingList(isEnabled: false);
        var context = new TransportLayerSecurityInterceptionContext(new MutableCertificateAuthorityProvider(new StubCertificateGenerator()), proxyingList);

        var first = await context.GetLeafCertificateAsync("alpha.example.com", CancellationToken.None);
        var second = await context.GetLeafCertificateAsync("beta.example.com", CancellationToken.None);

        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNotNull();
        await Assert.That(ReferenceEquals(first, second)).IsFalse();
    }
}