using Proxyfan.Domain.Certificates;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     A stub <see cref="ICertificateGenerator" /> that creates ephemeral self-signed
///     certificate authorities so view-model tests do not need to touch the certificate
///     store or call native APIs.
/// </summary>
internal sealed class StubCertificateGenerator : ICertificateGenerator
{
    private int _rootGenerationCount;

    public int RootGenerationCount => _rootGenerationCount;

    /// <inheritdoc />
    public Task<X509Certificate2> GenerateLeafCertificateAsync(
        string hostname,
        CertificateAuthority authority,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var certificate = authority.Sign(hostname);
        return Task.FromResult(certificate);
    }

    /// <inheritdoc />
    public Task<CertificateAuthority> GenerateRootCertificateAuthorityAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _rootGenerationCount);
        var authority = CreateAuthority();
        return Task.FromResult(authority);
    }

    private static CertificateAuthority CreateAuthority()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Proxyfan Client Test CA", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var constraints = new X509BasicConstraintsExtension(true, false, 0, true);
        var keyUsage = new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true);
        request.CertificateExtensions.Add(constraints);
        request.CertificateExtensions.Add(keyUsage);
        var notBefore = System.DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddDays(825);
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        var bytes = certificate.Export(X509ContentType.Pfx);
        var flags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet;
        var storedCertificate = X509CertificateLoader.LoadPkcs12(bytes, string.Empty, flags);
        var authority = new CertificateAuthority(storedCertificate);
        return authority;
    }
}
