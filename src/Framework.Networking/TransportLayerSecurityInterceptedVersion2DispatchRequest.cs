using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using System;
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
    ///     Gets the optional Remote Procedure Call (gRPC) store used to capture
    ///     <c>application/grpc</c> responses on the intercepted HTTP/2 connection. When
    ///     <see langword="null" />, gRPC traffic still tunnels correctly but captured messages
    ///     are not retained for inspection.
    /// </summary>
    public IRemoteProcedureCallStore? RemoteProcedureCallStore { get; init; }

    /// <summary>
    ///     Gets the decrypted upstream-facing stream (typically an authenticated
    ///     <see cref="System.Net.Security.SslStream" /> on the production path; any duplex
    ///     stream in tests).
    /// </summary>
    public required Stream ServerSecureStream { get; init; }

    /// <summary>
    ///     Gets the optional wall-clock time source for gRPC message timestamps. Defaults to
    ///     <see cref="System.TimeProvider.System" /> when not supplied.
    /// </summary>
    public TimeProvider? TimeProvider { get; init; }

    /// <summary>
    ///     Gets the store to deposit completed flows in.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }
}
