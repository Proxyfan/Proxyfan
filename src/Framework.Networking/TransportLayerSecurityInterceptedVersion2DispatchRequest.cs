using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using System.IO;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundles the arguments required by
///     <see cref="TransportLayerSecurityInterceptedVersion2Dispatch.RunAsync" />. Required by
///     the analyzer's 4-parameter limit (ATXCS022).
/// </summary>
public sealed class TransportLayerSecurityInterceptedVersion2DispatchRequest
{
    /// <summary>
    ///     Gets the decrypted client-facing stream (typically an authenticated
    ///     <see cref="System.Net.Security.SslStream" /> on the production path; any duplex
    ///     stream in tests).
    /// </summary>
    public required Stream ClientSecureStream { get; init; }

    /// <summary>
    ///     Gets the accepted proxy connection.
    /// </summary>
    public required IProxyConnection Connection { get; init; }

    /// <summary>
    ///     Gets the bus to publish capture events on.
    /// </summary>
    public required IDomainEventBus EventBus { get; init; }

    /// <summary>
    ///     Gets the decrypted upstream-facing stream (typically an authenticated
    ///     <see cref="System.Net.Security.SslStream" /> on the production path; any duplex
    ///     stream in tests).
    /// </summary>
    public required Stream ServerSecureStream { get; init; }

    /// <summary>
    ///     Gets the store to deposit completed flows in.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }
}
