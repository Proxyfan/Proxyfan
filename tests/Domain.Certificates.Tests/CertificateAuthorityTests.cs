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

    /// <summary>
    ///     Verifies that a leaf certificate generated for an IPv4 literal encodes the address as an
    ///     iPAddress Subject Alternative Name entry rather than a dNSName entry.
    /// </summary>
    [Test]
    public async Task Sign_WithIPv4Address_ReturnsLeafCertificateWithIpAddressSan()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);
        var expectedAddress = IPAddress.Parse("192.168.1.1");

        var leaf = authority.Sign("192.168.1.1");

        var san = leaf.Extensions
            .OfType<X509SubjectAlternativeNameExtension>()
            .Single();
        var ipAddresses = san.EnumerateIPAddresses().ToList();
        var dnsNames = san.EnumerateDnsNames().ToList();
        await Assert.That(ipAddresses).Contains(expectedAddress);
        await Assert.That(dnsNames).IsEmpty();
    }

    /// <summary>
    ///     Verifies that a leaf certificate generated for an IPv6 literal encodes the address as an
    ///     iPAddress Subject Alternative Name entry rather than a dNSName entry.
    /// </summary>
    [Test]
    public async Task Sign_WithIPv6Address_ReturnsLeafCertificateWithIpAddressSan()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);
        var expectedAddress = IPAddress.Parse("::1");

        var leaf = authority.Sign("::1");

        var san = leaf.Extensions
            .OfType<X509SubjectAlternativeNameExtension>()
            .Single();
        var ipAddresses = san.EnumerateIPAddresses().ToList();
        var dnsNames = san.EnumerateDnsNames().ToList();
        await Assert.That(ipAddresses).Contains(expectedAddress);
        await Assert.That(dnsNames).IsEmpty();
    }

    /// <summary>
    ///     Verifies that a leaf certificate generated for a DNS hostname encodes it as a dNSName
    ///     Subject Alternative Name entry and does not produce any iPAddress entries.
    /// </summary>
    [Test]
    public async Task Sign_WithDnsHostName_ReturnsLeafCertificateWithDnsNameSanOnly()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var leaf = authority.Sign("api.example.com");

        var san = leaf.Extensions
            .OfType<X509SubjectAlternativeNameExtension>()
            .Single();
        var ipAddresses = san.EnumerateIPAddresses().ToList();
        var dnsNames = san.EnumerateDnsNames().ToList();
        await Assert.That(dnsNames).Contains("api.example.com");
        await Assert.That(ipAddresses).IsEmpty();
    }

    private static async Task<CertificateAuthority> CreateAuthorityAsync(CancellationToken cancellationToken)
    {
        var generator = new RsaCertificateGenerator();
        return await generator.GenerateRootCertificateAuthorityAsync(cancellationToken).ConfigureAwait(false);
    }
}