using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.Proxy;
using System.Globalization;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model wrapping a single <see cref="ReverseProxyRoute" /> for display in the
///     reverse proxy tool window. Surfaces formatted route fields and the latest probe
///     status as observable properties.
/// </summary>
public sealed partial class ReverseProxyRouteViewModel : ObservableObject
{
    [ObservableProperty]
    private ReverseProxyRouteStatus _status;

    /// <summary>
    ///     Gets the formatted backend endpoint (host:port).
    /// </summary>
    public string BackendEndPoint { get; }

    /// <summary>
    ///     Gets the route identifier.
    /// </summary>
    public string Identifier => Route.Identifier;

    /// <summary>
    ///     Gets the formatted listen port (e.g. "127.0.0.1:9000").
    /// </summary>
    public string ListenEndPoint { get; }

    /// <summary>
    ///     Gets the human-readable route name.
    /// </summary>
    public string Name => Route.Name;

    /// <summary>
    ///     Gets the underlying domain route.
    /// </summary>
    public ReverseProxyRoute Route { get; }

    /// <summary>
    ///     Gets the TLS handling mode for the route.
    /// </summary>
    public ReverseProxyTransportLayerSecurityMode TransportLayerSecurityMode => Route.TransportLayerSecurityMode;

    /// <summary>
    ///     Initializes a new <see cref="ReverseProxyRouteViewModel" />.
    /// </summary>
    /// <param name="route">The underlying route.</param>
    /// <param name="status">The current status of the route.</param>
    public ReverseProxyRouteViewModel(ReverseProxyRoute route, ReverseProxyRouteStatus status)
    {
        Route = route;
        _status = status;
        ListenEndPoint = string.Create(CultureInfo.InvariantCulture, $"127.0.0.1:{route.ListenPort}");
        BackendEndPoint = string.Create(CultureInfo.InvariantCulture, $"{route.BackendHost}:{route.BackendPort}");
    }
}
