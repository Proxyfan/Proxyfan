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
    private const string SubjectAlternativeNameOid = "2.5.29.17";

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
    ///     Verifies that signing an IPv4 literal includes that IP address in the
    ///     Subject Alternative Names extension.
    /// </summary>
    [Test]
    public async Task Sign_WithIpv4LiteralHostName_AddsIpAddressSubjectAlternativeName()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var leaf = authority.Sign("127.0.0.1");
        var subjectAlternativeName = GetSubjectAlternativeNameExtension(leaf);
        var containsLoopbackAddress = subjectAlternativeName
            .EnumerateIPAddresses()
            .Any(ipAddress => ipAddress.Equals(IPAddress.Loopback));

        await Assert.That(containsLoopbackAddress).IsTrue();
    }

    /// <summary>
    ///     Verifies that signing a bracketed IPv6 literal includes that IP address in the
    ///     Subject Alternative Names extension.
    /// </summary>
    [Test]
    public async Task Sign_WithBracketedIpv6LiteralHostName_AddsIpAddressSubjectAlternativeName()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var leaf = authority.Sign("[::1]");
        var subjectAlternativeName = GetSubjectAlternativeNameExtension(leaf);
        var containsLoopbackAddress = subjectAlternativeName
            .EnumerateIPAddresses()
            .Any(ipAddress => ipAddress.Equals(IPAddress.IPv6Loopback));

        await Assert.That(containsLoopbackAddress).IsTrue();
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
    ///     Verifies that bracketed non-IPv6 host names are rejected.
    /// </summary>
    [Test]
    public async Task Sign_WithBracketedNonIpv6HostName_ThrowsArgumentException()
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(() => authority.Sign("[not-an-ipv6]")).Throws<ArgumentException>();
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
        var extension = certificate.Extensions
            .OfType<X509Extension>()
            .FirstOrDefault(item => item.Oid?.Value == SubjectAlternativeNameOid);
        if (extension is null)
        {
            throw new InvalidOperationException("Certificate is missing subject alternative name extension.");
        }

        return new X509SubjectAlternativeNameExtension(extension.RawData, extension.Critical);
    }
}