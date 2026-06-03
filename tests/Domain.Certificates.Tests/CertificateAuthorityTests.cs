using Proxyfan.Domain.Certificates;
using Proxyfan.Framework.Platform;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
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
        var subjectAlternativeNames = ReadSubjectAlternativeNames(leaf);

        await Assert.That(leaf.Issuer).IsEqualTo(authority.Certificate.Subject);
        await Assert.That(leaf.GetNameInfo(X509NameType.DnsName, false)).IsEqualTo("api.example.com");
        await Assert.That(subjectAlternativeNames.DnsNames.Count).IsEqualTo(1);
        await Assert.That(subjectAlternativeNames.DnsNames[0]).IsEqualTo("api.example.com");
        await Assert.That(subjectAlternativeNames.IpAddresses.Count).IsEqualTo(0);
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

        await Assert.That(() => authority.Sign("bad host")).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that IP-literal targets are encoded as IP-address Subject Alternative Names.
    /// </summary>
    [Test]
    [Arguments("127.0.0.1", "127.0.0.1")]
    [Arguments("2001:db8::1", "2001:db8::1")]
    [Arguments("[2001:db8::1]", "2001:db8::1")]
    public async Task Sign_WithIpLiteralHostName_ReturnsIpAddressSubjectAlternativeName(
        string hostname,
        string expectedIpAddress)
    {
        var authority = await CreateAuthorityAsync(CancellationToken.None).ConfigureAwait(false);

        var leaf = authority.Sign(hostname);
        var subjectAlternativeNames = ReadSubjectAlternativeNames(leaf);

        await Assert.That(subjectAlternativeNames.DnsNames.Count).IsEqualTo(0);
        await Assert.That(subjectAlternativeNames.IpAddresses.Count).IsEqualTo(1);
        await Assert.That(subjectAlternativeNames.IpAddresses[0]).IsEqualTo(IPAddress.Parse(expectedIpAddress));
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

    private static (List<string> DnsNames, List<IPAddress> IpAddresses) ReadSubjectAlternativeNames(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions["2.5.29.17"];
        if (extension is null)
        {
            return (new List<string>(), new List<IPAddress>());
        }

        var dnsNames = new List<string>();
        var ipAddresses = new List<IPAddress>();
        var reader = new AsnReader(extension.RawData, AsnEncodingRules.DER);
        var sequence = reader.ReadSequence();
        while (sequence.HasData)
        {
            var tag = sequence.PeekTag();
            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                var dnsName = sequence.ReadCharacterString(
                    UniversalTagNumber.IA5String,
                    new Asn1Tag(TagClass.ContextSpecific, 2));
                dnsNames.Add(dnsName);
                continue;
            }

            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 7)))
            {
                var addressBytes = sequence.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 7));
                ipAddresses.Add(new IPAddress(addressBytes));
                continue;
            }

            sequence.ReadEncodedValue();
        }

        reader.ThrowIfNotEmpty();
        return (dnsNames, ipAddresses);
    }
}