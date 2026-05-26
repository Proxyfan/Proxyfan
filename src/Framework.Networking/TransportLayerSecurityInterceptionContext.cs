using Proxyfan.Domain.Certificates;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Provides cached certificate material and proxying-list access for transport-layer-security interception.
/// </summary>
public sealed class TransportLayerSecurityInterceptionContext
{
    private readonly Lazy<Task<CertificateAuthority>> _certificateAuthorityTask;
    private readonly LeafCertificateCache _certificateCache;

    /// <summary>
    ///     Gets the server name indication proxying list that determines whether interception is enabled.
    /// </summary>
    public ServerNameIndicationProxyingList ProxyingList { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TransportLayerSecurityInterceptionContext" /> class.
    /// </summary>
    /// <param name="certificateGenerator">The certificate generator used to create the root authority.</param>
    /// <param name="proxyingList">The proxying rules used to decide when to intercept secure traffic.</param>
    public TransportLayerSecurityInterceptionContext(
        ICertificateGenerator certificateGenerator,
        ServerNameIndicationProxyingList proxyingList)
    {
        var certificateAuthorityTask = new Lazy<Task<CertificateAuthority>>(
            () => certificateGenerator.GenerateRootCertificateAuthorityAsync(CancellationToken.None));
        var certificateCache = new LeafCertificateCache(1000);
        _certificateAuthorityTask = certificateAuthorityTask;
        _certificateCache = certificateCache;
        ProxyingList = proxyingList;
    }

    /// <summary>
    ///     Gets or creates a cached leaf certificate for the specified host name.
    /// </summary>
    /// <param name="hostname">The host name for which to retrieve a leaf certificate.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The cached or newly signed certificate.</returns>
    public async Task<X509Certificate2> GetLeafCertificateAsync(string hostname, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var authority = await _certificateAuthorityTask.Value.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var certificate = _certificateCache.GetOrAdd(hostname, authority.Sign);
        return certificate;
    }
}
