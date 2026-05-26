using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Creates lightweight self-signed certificates for use by tests.
/// </summary>
internal static class CertificateTestFactory
{
    /// <summary>
    ///     Creates a short-lived self-signed certificate with the supplied subject common name.
    /// </summary>
    /// <param name="hostname">The common name to embed in the certificate subject.</param>
    /// <returns>A self-signed certificate suitable for tests.</returns>
    public static X509Certificate2 Create(string hostname)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={hostname}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var certificateBytes = certificate.Export(X509ContentType.Pfx);
        var loaded = X509CertificateLoader.LoadPkcs12(certificateBytes, string.Empty);
        return loaded;
    }
}