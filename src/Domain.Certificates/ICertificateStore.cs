using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Defines operations for installing and removing certificate authorities from a trust store.
/// </summary>
public interface ICertificateStore
{
    /// <summary>
    ///     Installs the specified certificate authority.
    /// </summary>
    /// <param name="authority">The certificate authority to install.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when installation has finished.</returns>
    Task InstallAsync(CertificateAuthority authority, CancellationToken cancellationToken);

    /// <summary>
    ///     Determines whether the specified certificate authority is installed.
    /// </summary>
    /// <param name="authority">The certificate authority to check.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    ///     <see langword="true" /> when the authority is installed; otherwise, <see langword="false" />.
    /// </returns>
    Task<bool> IsInstalledAsync(CertificateAuthority authority, CancellationToken cancellationToken);

    /// <summary>
    ///     Uninstalls the specified certificate authority.
    /// </summary>
    /// <param name="authority">The certificate authority to uninstall.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when uninstallation has finished.</returns>
    Task UninstallAsync(CertificateAuthority authority, CancellationToken cancellationToken);
}