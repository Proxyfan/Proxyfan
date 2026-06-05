using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
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
///     allow the user to add, edit, remove, start, stop, and probe reverse-proxy routes.
///     Subscribes to <see cref="IReverseProxyEngine.StatusChanged" /> so background probes
///     (e.g. <see cref="PeriodicReverseProxyHealthChecker" />) refresh the UI live.
/// </summary>
public sealed partial class ReverseProxySettingsViewModel : ObservableObject, IDisposable
{
    private const int DefaultBackendPort = 80;
    private const int DefaultListenPort = 9000;
    private readonly IReverseProxyEngine _engine;
    private readonly IOptionsMonitor<ProxyOptions>? _forwardProxyOptions;
    private readonly ReverseProxyRouteRegistry _registry;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string _backendHost;
    [ObservableProperty]
    private string _backendPort;
    [ObservableProperty]
    private string? _editingIdentifier;
    [ObservableProperty]
    private string _listenPort;
    [ObservableProperty]
    private string _routeName;
    [ObservableProperty]
    private ReverseProxyTransportLayerSecurityMode _transportLayerSecurityMode;
    [ObservableProperty]
    private string? _validationError;

    /// <summary>
    ///     Gets the observable list of configured routes (registry + engine status).
    /// </summary>
    public ObservableCollection<ReverseProxyRouteViewModel> Routes { get; }

    /// <summary>
    ///     Gets the supported TLS modes (used to populate the editor's combo-box).
    /// </summary>
    public IReadOnlyList<ReverseProxyTransportLayerSecurityMode> TransportLayerSecurityModes { get; }

    /// <summary>
    ///     Initializes a new <see cref="ReverseProxySettingsViewModel" /> bound to the supplied
    ///     registry, engine, and UI-thread scheduler. Port-conflict detection against the
    ///     forward proxy is disabled.
    /// </summary>
    /// <param name="registry">The route registry.</param>
    /// <param name="engine">The reverse proxy engine.</param>
    /// <param name="userInterfaceScheduler">The UI-thread scheduler.</param>
    public ReverseProxySettingsViewModel(
        ReverseProxyRouteRegistry registry,
        IReverseProxyEngine engine,
        IUserInterfaceScheduler userInterfaceScheduler)
        : this(registry, engine, userInterfaceScheduler, forwardProxyOptions: null)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="ReverseProxySettingsViewModel" /> bound to the supplied
    ///     registry, engine, and UI-thread scheduler. The forward-proxy options monitor is
    ///     optional; when supplied, the view model rejects routes whose listen port collides
    ///     with the forward proxy's port.
    /// </summary>
    /// <param name="registry">The route registry.</param>
    /// <param name="engine">The reverse proxy engine.</param>
    /// <param name="userInterfaceScheduler">The UI-thread scheduler.</param>
    /// <param name="forwardProxyOptions">Optional forward-proxy options for port-conflict detection.</param>
    public ReverseProxySettingsViewModel(
        ReverseProxyRouteRegistry registry,
        IReverseProxyEngine engine,
        IUserInterfaceScheduler userInterfaceScheduler,
        IOptionsMonitor<ProxyOptions>? forwardProxyOptions)
    {
        _registry = registry;
        _engine = engine;
        _userInterfaceScheduler = userInterfaceScheduler;
        _forwardProxyOptions = forwardProxyOptions;
        _routeName = string.Empty;
        _listenPort = DefaultListenPort.ToString(CultureInfo.InvariantCulture);
        _backendHost = string.Empty;
        _backendPort = DefaultBackendPort.ToString(CultureInfo.InvariantCulture);
        _transportLayerSecurityMode = ReverseProxyTransportLayerSecurityMode.None;
        TransportLayerSecurityModes =
        [
            ReverseProxyTransportLayerSecurityMode.None,
            ReverseProxyTransportLayerSecurityMode.Passthrough,
        ];
        Routes = [];
        ReloadRoutes();
        _engine.StatusChanged += OnEngineStatusChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _engine.StatusChanged -= OnEngineStatusChanged;
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
            ValidationError = "Route conflicts with an existing entry.";
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
    private void CancelEdit()
    {
        ResetEditor();
    }

    [RelayCommand]
    private void EditRoute(ReverseProxyRouteViewModel? route)
    {
        if (route is null)
        {
            return;
        }

        EditingIdentifier = route.Identifier;
        RouteName = route.Name;
        ListenPort = route.Route.ListenPort.ToString(CultureInfo.InvariantCulture);
        BackendHost = route.Route.BackendHost;
        BackendPort = route.Route.BackendPort.ToString(CultureInfo.InvariantCulture);
        TransportLayerSecurityMode = route.Route.TransportLayerSecurityMode;
        ValidationError = null;
    }

    private bool HasForwardProxyConflict(int listenPort)
    {
        if (_forwardProxyOptions is null)
        {
            return false;
        }

        var forwardPort = _forwardProxyOptions.CurrentValue.Port;
        return listenPort == forwardPort;
    }

    private bool HasPortParseError(string raw, string label, out int value)
    {
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            ValidationError = $"{label} port must be a number.";
            return true;
        }

        if (value is < 1 or > 65535)
        {
            ValidationError = $"{label} port must be between 1 and 65535.";
            return true;
        }

        return false;
    }

    private void OnEngineStatusChanged(string identifier, ReverseProxyRouteStatus status)
    {
        _userInterfaceScheduler.Post(() =>
        {
            foreach (var route in Routes)
            {
                if (string.Equals(route.Identifier, identifier, StringComparison.Ordinal))
                {
                    route.Status = status;
                    break;
                }
            }
        });
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
        if (string.Equals(EditingIdentifier, route.Identifier, StringComparison.Ordinal))
        {
            ResetEditor();
        }
    }

    private void ResetEditor()
    {
        RouteName = string.Empty;
        BackendHost = string.Empty;
        EditingIdentifier = null;
        ValidationError = null;
        TransportLayerSecurityMode = ReverseProxyTransportLayerSecurityMode.None;
    }

    [RelayCommand]
    private void SaveEdit()
    {
        var identifier = EditingIdentifier;
        if (identifier is null)
        {
            return;
        }

        var updated = TryBuildRoute(identifier);
        if (updated is null)
        {
            return;
        }

        if (!_registry.HasReplaced(identifier, updated))
        {
            ValidationError = "Route conflicts with an existing entry.";
            return;
        }

        for (var index = 0; index < Routes.Count; index++)
        {
            if (string.Equals(Routes[index].Identifier, identifier, StringComparison.Ordinal))
            {
                var status = Routes[index].Status;
                var replacementViewModel = new ReverseProxyRouteViewModel(updated, status);
                Routes[index] = replacementViewModel;
                break;
            }
        }

        ResetEditor();
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
        return TryBuildRouteCore(identifier: null);
    }

    private ReverseProxyRoute? TryBuildRoute(string identifier)
    {
        return TryBuildRouteCore(identifier);
    }

    private ReverseProxyRoute? TryBuildRouteCore(string? identifier)
    {
        ValidationError = null;
        var name = RouteName.Trim();
        if (name.Length == 0)
        {
            ValidationError = "Name is required.";
            return null;
        }

        var host = BackendHost.Trim();
        if (host.Length == 0)
        {
            ValidationError = "Backend host is required.";
            return null;
        }

        if (HasPortParseError(ListenPort, "Listen", out var parsedListenPort))
        {
            return null;
        }

        if (HasForwardProxyConflict(parsedListenPort))
        {
            ValidationError = "Listen port conflicts with the forward proxy.";
            return null;
        }

        if (HasPortParseError(BackendPort, "Backend", out var parsedBackendPort))
        {
            return null;
        }

        if (TransportLayerSecurityMode == ReverseProxyTransportLayerSecurityMode.Terminate)
        {
            ValidationError = "TLS termination is not yet supported. Use None or Passthrough.";
            return null;
        }

        var routeIdentifier = identifier ?? Guid.NewGuid().ToString("N");
        var route = new ReverseProxyRoute(
            routeIdentifier,
            name,
            parsedListenPort,
            host,
            parsedBackendPort,
            TransportLayerSecurityMode);
        return route;
    }
}
