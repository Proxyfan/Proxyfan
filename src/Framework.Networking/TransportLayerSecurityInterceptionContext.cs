using Proxyfan.Domain.Certificates;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Provides cached certificate material and proxying-list access for transport-layer-security interception.
/// </summary>
public sealed class TransportLayerSecurityInterceptionContext
{
    private readonly MutableCertificateAuthorityProvider _authorityProvider;
    private readonly LeafCertificateCache _certificateCache;

    /// <summary>
    ///     Gets the server name indication proxying list that determines whether interception is enabled.
    /// </summary>
    public ServerNameIndicationProxyingList ProxyingList { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TransportLayerSecurityInterceptionContext" /> class.
    /// </summary>
    /// <param name="authorityProvider">The provider that owns the current root certificate authority.</param>
    /// <param name="proxyingList">The proxying rules used to decide when to intercept secure traffic.</param>
    public TransportLayerSecurityInterceptionContext(
        MutableCertificateAuthorityProvider authorityProvider,
        ServerNameIndicationProxyingList proxyingList)
    {
        var certificateCache = new LeafCertificateCache(1000);
        _authorityProvider = authorityProvider;
        _certificateCache = certificateCache;
        ProxyingList = proxyingList;
        _authorityProvider.Changed += OnAuthorityChanged;
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
        var authority = await _authorityProvider.GetAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var certificate = _certificateCache.GetOrAdd(hostname, authority.Sign);
        return certificate;
    }

    private void OnAuthorityChanged(MutableCertificateAuthorityProvider sender)
    {
        _certificateCache.Clear();
    }
}
