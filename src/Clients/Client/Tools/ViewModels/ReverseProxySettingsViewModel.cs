using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Proxy;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Reverse Proxy tool window. Binds to the
///     <see cref="ReverseProxyRouteRegistry" /> and <see cref="IReverseProxyEngine" /> to
///     allow the user to add, remove, start, stop, and probe reverse-proxy routes.
/// </summary>
public sealed partial class ReverseProxySettingsViewModel : ObservableObject
{
    private const int DefaultBackendPort = 80;
    private const int DefaultListenPort = 9000;
    private readonly IReverseProxyEngine _engine;
    private readonly ReverseProxyRouteRegistry _registry;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string _backendHost;
    [ObservableProperty]
    private string _backendPort;
    [ObservableProperty]
    private string _listenPort;
    [ObservableProperty]
    private string _routeName;

    /// <summary>
    ///     Gets the observable list of configured routes (registry + engine status).
    /// </summary>
    public ObservableCollection<ReverseProxyRouteViewModel> Routes { get; }

    /// <summary>
    ///     Initializes a new <see cref="ReverseProxySettingsViewModel" /> bound to the supplied
    ///     registry, engine, and UI-thread scheduler.
    /// </summary>
    /// <param name="registry">The route registry.</param>
    /// <param name="engine">The reverse proxy engine.</param>
    /// <param name="userInterfaceScheduler">The UI-thread scheduler.</param>
    public ReverseProxySettingsViewModel(
        ReverseProxyRouteRegistry registry,
        IReverseProxyEngine engine,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _registry = registry;
        _engine = engine;
        _userInterfaceScheduler = userInterfaceScheduler;
        _routeName = string.Empty;
        _listenPort = DefaultListenPort.ToString(CultureInfo.InvariantCulture);
        _backendHost = string.Empty;
        _backendPort = DefaultBackendPort.ToString(CultureInfo.InvariantCulture);
        Routes = [];
        ReloadRoutes();
    }

    [RelayCommand]
    private void AddRoute()
    {
        var route = TryBuildRoute();
        if (route is null)
        {
            return;
        }

        if (!_registry.CanAdd(route))
        {
            return;
        }

        var addedViewModel = new ReverseProxyRouteViewModel(route, ReverseProxyRouteStatus.Stopped);
        Routes.Add(addedViewModel);
        ResetEditor();
    }

    private Dictionary<string, ReverseProxyRouteStatus> BuildStatusMap()
    {
        var states = _engine.GetStates();
        var map = new Dictionary<string, ReverseProxyRouteStatus>(states.Count, StringComparer.Ordinal);
        foreach (var state in states)
        {
            map[state.Route.Identifier] = state.Status;
        }

        return map;
    }

    [RelayCommand]
    private async Task ProbeAsync(ReverseProxyRouteViewModel? route, CancellationToken cancellationToken)
    {
        if (route is null)
        {
            return;
        }

        var status = await _engine.ProbeAsync(route.Identifier, cancellationToken).ConfigureAwait(false);
        _userInterfaceScheduler.Post(() => route.Status = status);
    }

    private void ReloadRoutes()
    {
        Routes.Clear();
        var statuses = BuildStatusMap();
        foreach (var configured in _registry.Routes)
        {
            var status = statuses.GetValueOrDefault(configured.Identifier, ReverseProxyRouteStatus.Stopped);
            var configuredViewModel = new ReverseProxyRouteViewModel(configured, status);
            Routes.Add(configuredViewModel);
        }
    }

    [RelayCommand]
    private void RemoveRoute(ReverseProxyRouteViewModel? route)
    {
        if (route is null)
        {
            return;
        }

        _ = _registry.HasRemoved(route.Identifier);
        Routes.Remove(route);
    }

    private void ResetEditor()
    {
        RouteName = string.Empty;
        BackendHost = string.Empty;
    }

    [RelayCommand]
    private async Task StartRouteAsync(ReverseProxyRouteViewModel? route, CancellationToken cancellationToken)
    {
        if (route is null)
        {
            return;
        }

        var started = await _engine.StartRouteAsync(route.Route, cancellationToken).ConfigureAwait(false);
        ReverseProxyRouteStatus nextStatus;
        if (started)
        {
            nextStatus = ReverseProxyRouteStatus.Healthy;
        }
        else
        {
            nextStatus = ReverseProxyRouteStatus.Faulted;
        }

        _userInterfaceScheduler.Post(() => route.Status = nextStatus);
    }

    [RelayCommand]
    private async Task StopRouteAsync(ReverseProxyRouteViewModel? route, CancellationToken cancellationToken)
    {
        if (route is null)
        {
            return;
        }

        var stopped = await _engine.StopRouteAsync(route.Identifier, cancellationToken).ConfigureAwait(false);
        if (stopped)
        {
            _userInterfaceScheduler.Post(() => route.Status = ReverseProxyRouteStatus.Stopped);
        }
    }

    private ReverseProxyRoute? TryBuildRoute()
    {
        var name = RouteName.Trim();
        if (name.Length == 0)
        {
            return null;
        }

        var host = BackendHost.Trim();
        if (host.Length == 0)
        {
            return null;
        }

        if (!int.TryParse(ListenPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedListenPort))
        {
            return null;
        }

        if (parsedListenPort is < 1 or > 65535)
        {
            return null;
        }

        if (!int.TryParse(BackendPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedBackendPort))
        {
            return null;
        }

        if (parsedBackendPort is < 1 or > 65535)
        {
            return null;
        }

        var identifier = Guid.NewGuid().ToString("N");
        var route = new ReverseProxyRoute(
            identifier,
            name,
            parsedListenPort,
            host,
            parsedBackendPort,
            ReverseProxyTransportLayerSecurityMode.None);
        return route;
    }
}
