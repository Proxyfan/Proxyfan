using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Represents a root certificate authority used to issue leaf certificates
///     for intercepted secure connections.
/// </summary>
public sealed class CertificateAuthority
{
    /// <summary>
    ///     Gets the X509 certificate for this authority.
    /// </summary>
    public X509Certificate2 Certificate { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether this authority is installed in the platform trust store.
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CertificateAuthority" /> class.
    /// </summary>
    /// <param name="certificate">The root certificate authority certificate.</param>
    public CertificateAuthority(X509Certificate2 certificate)
    {
        Certificate = certificate;
        IsInstalled = false;
    }

    /// <summary>
    ///     Generates a leaf certificate for the specified host name signed by this authority.
    /// </summary>
    /// <param name="hostname">The host name to include in the generated certificate.</param>
    /// <returns>The generated leaf certificate.</returns>
    public X509Certificate2 Sign(string hostname)
    {
        return CreateLeafCertificate(hostname);
    }

    private X509Certificate2 CreateLeafCertificate(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new ArgumentException("Leaf certificate host name must be provided.", nameof(hostname));
        }

        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={hostname}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var constraints = new X509BasicConstraintsExtension(false, false, 0, true);
        var keyUsage = new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true);
        var serverAuthenticationUsage = CreateServerAuthenticationUsage();
        var serverNameIndicationExtension = CreateServerNameIndicationExtension(hostname);
        var subjectKeyIdentifier = new X509SubjectKeyIdentifierExtension(request.PublicKey, false);
        request.CertificateExtensions.Add(constraints);
        request.CertificateExtensions.Add(keyUsage);
        request.CertificateExtensions.Add(serverAuthenticationUsage);
        request.CertificateExtensions.Add(serverNameIndicationExtension);
        request.CertificateExtensions.Add(subjectKeyIdentifier);

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddDays(365);
        var serialNumber = RandomNumberGenerator.GetBytes(16);
        using var certificate = request.Create(Certificate, notBefore, notAfter, serialNumber);
        using var certificateWithPrivateKey = certificate.CopyWithPrivateKey(key);
        var certificateBytes = certificateWithPrivateKey.Export(X509ContentType.Pfx);
        var storageFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet;
        var leafCertificate = X509CertificateLoader.LoadPkcs12(certificateBytes, string.Empty, storageFlags);
        return leafCertificate;
    }

    private X509EnhancedKeyUsageExtension CreateServerAuthenticationUsage()
    {
        var serverAuthenticationIdentifier = new Oid("1.3.6.1.5.5.7.3.1");
        var usageIdentifiers = new OidCollection
        {
            serverAuthenticationIdentifier,
        };
        var usage = new X509EnhancedKeyUsageExtension(usageIdentifiers, true);
        return usage;
    }

    private X509Extension CreateServerNameIndicationExtension(string hostname)
    {
        var builder = new SubjectAlternativeNameBuilder();
        if (IPAddress.TryParse(hostname, out var ipAddress))
        {
            builder.AddIpAddress(ipAddress);
        }
        else
        {
            builder.AddDnsName(hostname);
        }

        var extension = builder.Build();
        return extension;
    }
}