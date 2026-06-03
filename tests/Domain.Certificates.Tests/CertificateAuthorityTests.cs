using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Platform;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Tests for <see cref="CertificateAuthority" />.
/// </summary>
public sealed class CertificateAuthorityTests
{
    /// <summary>
    ///     Verifies that a newly constructed <see cref="CertificateAuthority" /> reports
    ///     <see cref="CertificateAuthority.IsInstalled" /> as <see langword="false" />.
    /// </summary>
    [Test]
    public async Task Constructor_WhenCreated_IsInstalledIsFalse()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(authority.IsInstalled).IsFalse();
    }

    /// <summary>
    ///     Verifies that the generated leaf certificate is issued by this authority and
    ///     contains the requested host name in the Subject Alternative Names extension.
    /// </summary>
    [Test]
    public async Task Sign_WithValidHostName_ReturnsLeafCertificateIssuedByAuthority()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var leaf = authority.Sign("api.example.com");

        await Assert.That(leaf.Issuer).IsEqualTo(authority.Certificate.Subject);
        await Assert.That(leaf.GetNameInfo(X509NameType.DnsName, false)).IsEqualTo("api.example.com");
        await Assert.That(GetSubjectAlternativeNameExtension(leaf).EnumerateDnsNames().ToArray()).IsEquivalentTo(["api.example.com"]);
    }

    /// <summary>
    ///     Verifies that the generated leaf certificate stores IP-literal targets as IP Subject
    ///     Alternative Names.
    /// </summary>
    [Test]
    [Arguments("127.0.0.1", "127.0.0.1")]
    [Arguments("[2001:db8::1]", "2001:db8::1")]
    public async Task Sign_WithIpAddressLiteral_ReturnsLeafCertificateWithIpAddressSubjectAlternativeName(string hostname, string expectedIpAddress)
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var leaf = authority.Sign(hostname);
        var subjectAlternativeName = GetSubjectAlternativeNameExtension(leaf);
        var ipAddresses = subjectAlternativeName.EnumerateIPAddresses().ToArray();

        await Assert.That(ipAddresses.Length).IsEqualTo(1);
        await Assert.That(ipAddresses[0]).IsEqualTo(IPAddress.Parse(expectedIpAddress));
        await Assert.That(subjectAlternativeName.EnumerateDnsNames().Any()).IsFalse();
    }

    /// <summary>
    ///     Verifies that the generated leaf certificate normalizes domain host names before
    ///     encoding the Subject Alternative Name extension.
    /// </summary>
    [Test]
    [Arguments("api.example.com.", "api.example.com")]
    [Arguments("münich.example", "xn--mnich-kva.example")]
    public async Task Sign_WithNormalizedDomainHostName_ReturnsLeafCertificateWithNormalizedDnsSubjectAlternativeName(string hostname, string expectedHostName)
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var leaf = authority.Sign(hostname);
        var dnsNames = GetSubjectAlternativeNameExtension(leaf).EnumerateDnsNames().ToArray();

        await Assert.That(dnsNames.Length).IsEqualTo(1);
        await Assert.That(dnsNames[0]).IsEqualTo(expectedHostName);
        await Assert.That(leaf.Subject).IsEqualTo($"CN={expectedHostName}");
    }

    /// <summary>
    ///     Verifies that the generated leaf certificate has a private key attached.
    /// </summary>
    [Test]
    public async Task Sign_WithValidHostName_ReturnsLeafCertificateWithPrivateKey()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var leaf = authority.Sign("secure.example.com");

        await Assert.That(leaf.HasPrivateKey).IsTrue();
    }

    /// <summary>
    ///     Verifies that the generated leaf certificate does NOT have the certificate-authority
    ///     basic constraint set.
    /// </summary>
    [Test]
    public async Task Sign_WithValidHostName_ReturnsNonCaLeafCertificate()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var leaf = authority.Sign("leaf.example.com");

        var constraint = leaf.Extensions
            .OfType<X509BasicConstraintsExtension>()
            .FirstOrDefault();
        await Assert.That(constraint).IsNotNull();
        await Assert.That(constraint!.CertificateAuthority).IsFalse();
    }

    /// <summary>
    ///     Verifies that calling <see cref="CertificateAuthority.Sign" /> with a null or whitespace
    ///     host name throws <see cref="ArgumentException" />.
    /// </summary>
    [Test]
    public async Task Sign_WithEmptyHostName_ThrowsArgumentException()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(() => authority.Sign(string.Empty)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that calling <see cref="CertificateAuthority.Sign" /> with an invalid host
    ///     name throws <see cref="ArgumentException" />.
    /// </summary>
    [Test]
    public async Task Sign_WithInvalidHostName_ThrowsArgumentException()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(() => authority.Sign("bad host name")).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that two calls to <see cref="CertificateAuthority.Sign" /> for the same host
    ///     return distinct certificate instances.
    /// </summary>
    [Test]
    public async Task Sign_CalledTwiceForSameHost_ReturnsDifferentInstances()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var firstLeaf = authority.Sign("repeated.example.com");
        var secondLeaf = authority.Sign("repeated.example.com");

        await Assert.That(firstLeaf).IsNotSameReferenceAs(secondLeaf);
    }

    private static async Task<CertificateAuthority> CreateAuthorityAsync(CancellationToken cancellationToken)
    {
        var generator = new RsaCertificateGenerator();
        return await generator.GenerateRootCertificateAuthorityAsync(cancellationToken).ConfigureAwait(false);
    }

    private static X509SubjectAlternativeNameExtension GetSubjectAlternativeNameExtension(X509Certificate2 certificate)
    {
        foreach (var extension in certificate.Extensions)
        {
            if (extension is X509SubjectAlternativeNameExtension subjectAlternativeNameExtension)
            {
                return subjectAlternativeNameExtension;
            }
        }

        throw new InvalidOperationException($"Expected subject alternative name extension on certificate '{certificate.Subject}'.");
    }
}