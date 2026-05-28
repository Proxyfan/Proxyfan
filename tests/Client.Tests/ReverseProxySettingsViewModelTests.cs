using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Proxy;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ReverseProxySettingsViewModel" /> covering route editor, route
///     list management, and engine command delegation.
/// </summary>
public sealed class ReverseProxySettingsViewModelTests
{
    /// <summary>
    ///     Adding a route with valid editor values appends a new entry to <see cref="ReverseProxySettingsViewModel.Routes" />
    ///     and resets the name and host editor fields.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_ValidEditor_AppendsRouteAndResetsEditor()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "api.example.com",
            BackendPort = "443",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(1);
        await Assert.That(viewModel.Routes[0].Name).IsEqualTo("Api");
        await Assert.That(viewModel.Routes[0].ListenEndPoint).IsEqualTo("127.0.0.1:9100");
        await Assert.That(viewModel.Routes[0].BackendEndPoint).IsEqualTo("api.example.com:443");
        await Assert.That(viewModel.RouteName).IsEqualTo(string.Empty);
        await Assert.That(viewModel.BackendHost).IsEqualTo(string.Empty);
        await Assert.That(registry.Routes.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Adding a route with an empty name does not add anything.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_EmptyName_DoesNothing()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "   ",
            ListenPort = "9100",
            BackendHost = "host",
            BackendPort = "80",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Adding a route with a non-numeric listen port does not add anything.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_InvalidPort_DoesNothing()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "not-a-port",
            BackendHost = "host",
            BackendPort = "80",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Adding a route with an out-of-range backend port does not add anything.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_PortOutOfRange_DoesNothing()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "host",
            BackendPort = "70000",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Removing a route deletes it from both the view model collection and the underlying registry.
    /// </summary>
    [Test]
    public async Task RemoveRouteCommand_KnownRoute_RemovesFromRoutesAndRegistry()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "host",
            BackendPort = "80",
        };
        viewModel.AddRouteCommand.Execute(null);
        var added = viewModel.Routes[0];

        viewModel.RemoveRouteCommand.Execute(added);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
        await Assert.That(registry.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Starting a known route delegates to <see cref="IReverseProxyEngine.StartRouteAsync" /> and
    ///     updates the route status to <see cref="ReverseProxyRouteStatus.Healthy" /> on success.
    /// </summary>
    [Test]
    public async Task StartRouteCommand_Success_UpdatesStatusToHealthy()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine { NextStartResult = true };
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "host",
            BackendPort = "80",
        };
        viewModel.AddRouteCommand.Execute(null);
        var added = viewModel.Routes[0];

        await viewModel.StartRouteCommand.ExecuteAsync(added);

        await Assert.That(added.Status).IsEqualTo(ReverseProxyRouteStatus.Healthy);
    }

    /// <summary>
    ///     Starting a route that the engine refuses sets status to <see cref="ReverseProxyRouteStatus.Faulted" />.
    /// </summary>
    [Test]
    public async Task StartRouteCommand_Failure_UpdatesStatusToFaulted()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine { NextStartResult = false };
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "host",
            BackendPort = "80",
        };
        viewModel.AddRouteCommand.Execute(null);
        var added = viewModel.Routes[0];

        await viewModel.StartRouteCommand.ExecuteAsync(added);

        await Assert.That(added.Status).IsEqualTo(ReverseProxyRouteStatus.Faulted);
    }

    /// <summary>
    ///     Stopping a started route sets status to <see cref="ReverseProxyRouteStatus.Stopped" />.
    /// </summary>
    [Test]
    public async Task StopRouteCommand_StartedRoute_UpdatesStatusToStopped()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "host",
            BackendPort = "80",
        };
        viewModel.AddRouteCommand.Execute(null);
        var added = viewModel.Routes[0];
        await viewModel.StartRouteCommand.ExecuteAsync(added);

        await viewModel.StopRouteCommand.ExecuteAsync(added);

        await Assert.That(added.Status).IsEqualTo(ReverseProxyRouteStatus.Stopped);
    }

    /// <summary>
    ///     Probing a known route delegates to the engine and reflects the returned status.
    /// </summary>
    [Test]
    public async Task ProbeCommand_KnownRoute_UpdatesStatusFromEngine()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine { NextProbeStatus = ReverseProxyRouteStatus.Unhealthy };
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "host",
            BackendPort = "80",
        };
        viewModel.AddRouteCommand.Execute(null);
        var added = viewModel.Routes[0];
        await viewModel.StartRouteCommand.ExecuteAsync(added);

        await viewModel.ProbeCommand.ExecuteAsync(added);

        await Assert.That(engine.ProbeCallCount).IsEqualTo(1);
        await Assert.That(added.Status).IsEqualTo(ReverseProxyRouteStatus.Unhealthy);
    }

    /// <summary>
    ///     Probing a null route is a no-op.
    /// </summary>
    [Test]
    public async Task ProbeCommand_NullRoute_DoesNotCallEngine()
    {
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(new ReverseProxyRouteRegistry(), engine, InlineUserInterfaceScheduler.Instance);

        await viewModel.ProbeCommand.ExecuteAsync(null);

        await Assert.That(engine.ProbeCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Removing a null route is a no-op (no exception).
    /// </summary>
    [Test]
    public async Task RemoveRouteCommand_NullArgument_DoesNothing()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance);

        viewModel.RemoveRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     The view model surfaces existing registry routes on construction with the matching engine status.
    /// </summary>
    [Test]
    public async Task Construction_PreexistingRegistryRoutes_PopulatesRoutes()
    {
        var registry = new ReverseProxyRouteRegistry();
        var preexisting = new ReverseProxyRoute(
            "abc",
            "Preexisting",
            8500,
            "example.com",
            443,
            ReverseProxyTransportLayerSecurityMode.None);
        _ = registry.CanAdd(preexisting);
        var engine = new StubReverseProxyEngine();

        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(1);
        await Assert.That(viewModel.Routes[0].Identifier).IsEqualTo("abc");
        await Assert.That(viewModel.Routes[0].Status).IsEqualTo(ReverseProxyRouteStatus.Stopped);
    }

    /// <summary>
    ///     Adding a route with a blank backend host is rejected.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_BlankHost_DoesNothing()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "   ",
            BackendPort = "80",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Adding a route with a listen port outside the valid range is rejected.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_ListenPortOutOfRange_DoesNothing()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "0",
            BackendHost = "host",
            BackendPort = "80",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Adding a route with an unparseable backend port is rejected.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_BackendPortNotANumber_DoesNothing()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "host",
            BackendPort = "abc",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Adding a route that conflicts with an existing identifier short-circuits at CanAdd.
    ///     Achieved by pre-seeding the registry with a route whose identifier matches what
    ///     CanAdd will detect (same listen port + backend tuple counts as duplicate).
    /// </summary>
    [Test]
    public async Task AddRouteCommand_DuplicateListenPort_DoesNothing()
    {
        var registry = new ReverseProxyRouteRegistry();
        var pre = new ReverseProxyRoute("pre", "Pre", 9100, "host", 80, ReverseProxyTransportLayerSecurityMode.None);
        _ = registry.CanAdd(pre);
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Conflict",
            ListenPort = "9100",
            BackendHost = "other.host",
            BackendPort = "80",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(1);
        await Assert.That(viewModel.Routes[0].Identifier).IsEqualTo("pre");
    }

    /// <summary>
    ///     StartRouteCommand is a no-op for a null route argument.
    /// </summary>
    [Test]
    public async Task StartRouteCommand_NullRoute_DoesNothing()
    {
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(new ReverseProxyRouteRegistry(), engine, InlineUserInterfaceScheduler.Instance);

        await viewModel.StartRouteCommand.ExecuteAsync(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     StopRouteCommand is a no-op for a null route argument.
    /// </summary>
    [Test]
    public async Task StopRouteCommand_NullRoute_DoesNothing()
    {
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(new ReverseProxyRouteRegistry(), engine, InlineUserInterfaceScheduler.Instance);

        await viewModel.StopRouteCommand.ExecuteAsync(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     StopRouteCommand leaves the route status untouched when the engine reports the route was unknown.
    /// </summary>
    [Test]
    public async Task StopRouteCommand_EngineReturnsFalse_DoesNotChangeStatus()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "host",
            BackendPort = "80",
        };
        viewModel.AddRouteCommand.Execute(null);
        var added = viewModel.Routes[0];
        added.Status = ReverseProxyRouteStatus.Faulted;

        await viewModel.StopRouteCommand.ExecuteAsync(added);

        await Assert.That(added.Status).IsEqualTo(ReverseProxyRouteStatus.Faulted);
    }
}
