using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Tests for <see cref="RsaCertificateGenerator" />.
/// </summary>
public sealed class RsaCertificateGeneratorTests
{
    /// <summary>
    ///     Verifies that generating a leaf certificate uses the provided authority and host name.
    /// </summary>
    [Test]
    public async Task GenerateLeafCertificateAsync_WhenHostNameIsProvided_ReturnsLeafCertificate()
    {
        var generator = new RsaCertificateGenerator();
        var authority = await generator.GenerateRootCertificateAuthorityAsync(CancellationToken.None);

        var certificate = await generator.GenerateLeafCertificateAsync("api.example.com", authority, CancellationToken.None);

        await Assert.That(certificate.Issuer).IsEqualTo(authority.Certificate.Subject);
        await Assert.That(certificate.GetNameInfo(X509NameType.DnsName, false)).IsEqualTo("api.example.com");
        await Assert.That(certificate.HasPrivateKey).IsTrue();
        await Assert.That(HasCertificateAuthority(certificate)).IsFalse();
    }

    /// <summary>
    ///     Verifies that generating a root certificate authority creates a certificate authority certificate.
    /// </summary>
    [Test]
    public async Task GenerateRootCertificateAuthorityAsync_WhenInvoked_ReturnsCertificateAuthority()
    {
        var generator = new RsaCertificateGenerator();

        var authority = await generator.GenerateRootCertificateAuthorityAsync(CancellationToken.None);

        await Assert.That(authority.Certificate.Subject).IsEqualTo("CN=Proxyfan Certificate Authority");
        await Assert.That(authority.Certificate.HasPrivateKey).IsTrue();
        await Assert.That(HasCertificateAuthority(authority.Certificate)).IsTrue();
    }

    private static bool HasCertificateAuthority(X509Certificate2 certificate)
    {
        X509BasicConstraintsExtension? extension = certificate.Extensions
            .OfType<X509BasicConstraintsExtension>()
            .FirstOrDefault();

        if (extension is null)
        {
            return false;
        }

        return extension.CertificateAuthority;
    }
}