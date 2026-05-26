using Proxyfan.Domain.Certificates;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Generates root and leaf certificates backed by RSA keys.
/// </summary>
public sealed class RsaCertificateGenerator : ICertificateGenerator
{
    /// <inheritdoc />
    public Task<X509Certificate2> GenerateLeafCertificateAsync(
        string hostname,
        CertificateAuthority authority,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(authority.Sign(hostname));
    }

    /// <inheritdoc />
    public Task<CertificateAuthority> GenerateRootCertificateAuthorityAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var authority = CreateRootCertificateAuthority();
        return Task.FromResult(authority);
    }

    private CertificateAuthority CreateRootCertificateAuthority()
    {
        using var key = RSA.Create(4096);
        var request = new CertificateRequest("CN=Proxyfan Certificate Authority", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var constraints = new X509BasicConstraintsExtension(true, false, 0, true);
        var keyUsage = new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true);
        var subjectKeyIdentifier = new X509SubjectKeyIdentifierExtension(request.PublicKey, false);
        request.CertificateExtensions.Add(constraints);
        request.CertificateExtensions.Add(keyUsage);
        request.CertificateExtensions.Add(subjectKeyIdentifier);

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddDays(825);
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        var certificateBytes = certificate.Export(X509ContentType.Pfx);
        var storageFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet;
        var storedCertificate = X509CertificateLoader.LoadPkcs12(certificateBytes, string.Empty, storageFlags);
        var authority = new CertificateAuthority(storedCertificate);
        return authority;
    }
}