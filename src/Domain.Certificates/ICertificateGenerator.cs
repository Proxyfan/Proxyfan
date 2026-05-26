using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Defines certificate generation operations for root authorities and leaf certificates.
/// </summary>
public interface ICertificateGenerator
{
    /// <summary>
    ///     Generates a new leaf certificate for the specified host name.
    /// </summary>
    /// <param name="hostname">The host name to include in the certificate.</param>
    /// <param name="authority">The certificate authority that signs the leaf certificate.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The generated leaf certificate.</returns>
    Task<X509Certificate2> GenerateLeafCertificateAsync(string hostname, CertificateAuthority authority, CancellationToken cancellationToken);

    /// <summary>
    ///     Generates a new root certificate authority.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The generated certificate authority.</returns>
    Task<CertificateAuthority> GenerateRootCertificateAuthorityAsync(CancellationToken cancellationToken);
}