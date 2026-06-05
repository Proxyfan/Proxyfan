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

    /// <summary>
    ///     When the view model is constructed against an engine that already reports route state,
    ///     <c>ReloadRoutes</c> seeds each route's status from the engine. Covers the non-empty
    ///     branch of <c>BuildStatusMap</c>.
    /// </summary>
    [Test]
    public async Task Construction_EngineReportsRouteState_SeedsRouteStatusFromEngine()
    {
        var registry = new ReverseProxyRouteRegistry();
        var route = new ReverseProxyRoute(
            "preseeded",
            "Preseeded",
            9300,
            "host",
            80,
            ReverseProxyTransportLayerSecurityMode.None);
        _ = registry.CanAdd(route);

        var engine = new StubReverseProxyEngine { NextStartResult = true };
        await engine.StartRouteAsync(route, CancellationToken.None);

        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(1);
        await Assert.That(viewModel.Routes[0].Status).IsEqualTo(ReverseProxyRouteStatus.Healthy);
    }

    /// <summary>
    ///     The view model exposes TLS modes for the editor combo box and defaults to None.
    /// </summary>
    [Test]
    public async Task TransportLayerSecurityMode_DefaultEditor_StartsAsNone()
    {
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(
            new ReverseProxyRouteRegistry(),
            engine,
            InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.TransportLayerSecurityMode).IsEqualTo(ReverseProxyTransportLayerSecurityMode.None);
        await Assert.That(viewModel.TransportLayerSecurityModes.Count).IsEqualTo(2);
        await Assert.That(viewModel.TransportLayerSecurityModes).Contains(ReverseProxyTransportLayerSecurityMode.None);
        await Assert.That(viewModel.TransportLayerSecurityModes).Contains(ReverseProxyTransportLayerSecurityMode.Passthrough);
        await Assert.That(viewModel.TransportLayerSecurityModes).DoesNotContain(ReverseProxyTransportLayerSecurityMode.Terminate);
    }

    /// <summary>
    ///     Adding a route with the editor TLS mode set to Terminate is rejected with a validation error.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_TerminateTls_SetsValidationError()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "api.example.com",
            BackendPort = "443",
            TransportLayerSecurityMode = ReverseProxyTransportLayerSecurityMode.Terminate,
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(registry.Routes.Count).IsEqualTo(0);
        await Assert.That(viewModel.ValidationError).IsNotNull();
    }

    /// <summary>
    ///     Adding a route that collides with the forward proxy port surfaces the validation error.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_PortConflictsWithForwardProxy_SetsValidationError()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var forwardProxyOptions = new StubOptionsMonitor<ProxyOptions>(new ProxyOptions { Port = 8080 });
        var viewModel = new ReverseProxySettingsViewModel(
            registry,
            engine,
            InlineUserInterfaceScheduler.Instance,
            forwardProxyOptions)
        {
            RouteName = "Api",
            ListenPort = "8080",
            BackendHost = "api.example.com",
            BackendPort = "443",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
        await Assert.That(viewModel.ValidationError).IsNotNull();
    }

    /// <summary>
    ///     Adding a route with a duplicate listen port (registry rejection) populates ValidationError.
    /// </summary>
    [Test]
    public async Task AddRouteCommand_RegistryRejectsDuplicate_SetsValidationError()
    {
        var registry = new ReverseProxyRouteRegistry();
        _ = registry.CanAdd(new ReverseProxyRoute("pre", "Pre", 9100, "host", 80, ReverseProxyTransportLayerSecurityMode.None));
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Conflict",
            ListenPort = "9100",
            BackendHost = "other.host",
            BackendPort = "80",
        };

        viewModel.AddRouteCommand.Execute(null);

        await Assert.That(viewModel.ValidationError).IsNotNull();
    }

    /// <summary>
    ///     EditRouteCommand populates the editor fields from the selected route and switches to save mode.
    /// </summary>
    [Test]
    public async Task EditRouteCommand_KnownRoute_PopulatesEditorAndEntersEditMode()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance)
        {
            RouteName = "Api",
            ListenPort = "9100",
            BackendHost = "api.example.com",
            BackendPort = "443",
            TransportLayerSecurityMode = ReverseProxyTransportLayerSecurityMode.Passthrough,
        };
        viewModel.AddRouteCommand.Execute(null);
        var existing = viewModel.Routes[0];

        viewModel.EditRouteCommand.Execute(existing);

        await Assert.That(viewModel.EditingIdentifier).IsEqualTo(existing.Identifier);
        await Assert.That(viewModel.RouteName).IsEqualTo("Api");
        await Assert.That(viewModel.ListenPort).IsEqualTo("9100");
        await Assert.That(viewModel.BackendHost).IsEqualTo("api.example.com");
        await Assert.That(viewModel.BackendPort).IsEqualTo("443");
        await Assert.That(viewModel.TransportLayerSecurityMode).IsEqualTo(ReverseProxyTransportLayerSecurityMode.Passthrough);
    }

    /// <summary>
    ///     EditRouteCommand with a null argument is a no-op.
    /// </summary>
    [Test]
    public async Task EditRouteCommand_NullArgument_DoesNothing()
    {
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(new ReverseProxyRouteRegistry(), engine, InlineUserInterfaceScheduler.Instance);

        viewModel.EditRouteCommand.Execute(null);

        await Assert.That(viewModel.EditingIdentifier).IsNull();
    }

    /// <summary>
    ///     SaveEditCommand applies edits in place and resets the editor.
    /// </summary>
    [Test]
    public async Task SaveEditCommand_ChangedFields_UpdatesRouteAndResetsEditor()
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
        var existing = viewModel.Routes[0];

        viewModel.EditRouteCommand.Execute(existing);
        viewModel.RouteName = "Api v2";
        viewModel.BackendHost = "api.v2.example.com";
        viewModel.SaveEditCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(1);
        await Assert.That(viewModel.Routes[0].Name).IsEqualTo("Api v2");
        await Assert.That(viewModel.Routes[0].Route.BackendHost).IsEqualTo("api.v2.example.com");
        await Assert.That(viewModel.EditingIdentifier).IsNull();
        await Assert.That(registry.Routes[0].Name).IsEqualTo("Api v2");
    }

    /// <summary>
    ///     SaveEditCommand without a current edit is a no-op.
    /// </summary>
    [Test]
    public async Task SaveEditCommand_NotEditing_DoesNothing()
    {
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(new ReverseProxyRouteRegistry(), engine, InlineUserInterfaceScheduler.Instance);

        viewModel.SaveEditCommand.Execute(null);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     SaveEditCommand sets ValidationError when the editor validation fails (e.g., empty name).
    /// </summary>
    [Test]
    public async Task SaveEditCommand_InvalidEditor_SetsValidationError()
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
        var existing = viewModel.Routes[0];
        viewModel.EditRouteCommand.Execute(existing);

        viewModel.RouteName = string.Empty;
        viewModel.SaveEditCommand.Execute(null);

        await Assert.That(viewModel.ValidationError).IsNotNull();
        await Assert.That(viewModel.EditingIdentifier).IsEqualTo(existing.Identifier);
    }

    /// <summary>
    ///     SaveEditCommand surfaces a validation error when the replacement port collides with another route.
    /// </summary>
    [Test]
    public async Task SaveEditCommand_PortCollidesWithOtherRoute_SetsValidationError()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance);

        viewModel.RouteName = "A";
        viewModel.ListenPort = "9000";
        viewModel.BackendHost = "host.a";
        viewModel.BackendPort = "80";
        viewModel.AddRouteCommand.Execute(null);

        viewModel.RouteName = "B";
        viewModel.ListenPort = "9001";
        viewModel.BackendHost = "host.b";
        viewModel.BackendPort = "80";
        viewModel.AddRouteCommand.Execute(null);

        var second = viewModel.Routes[1];
        viewModel.EditRouteCommand.Execute(second);
        viewModel.ListenPort = "9000";
        viewModel.SaveEditCommand.Execute(null);

        await Assert.That(viewModel.ValidationError).IsNotNull();
        await Assert.That(registry.Routes[1].ListenPort).IsEqualTo(9001);
    }

    /// <summary>
    ///     CancelEditCommand clears the editor and the editing identifier.
    /// </summary>
    [Test]
    public async Task CancelEditCommand_DuringEdit_ResetsEditor()
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
        viewModel.EditRouteCommand.Execute(viewModel.Routes[0]);

        viewModel.CancelEditCommand.Execute(null);

        await Assert.That(viewModel.EditingIdentifier).IsNull();
        await Assert.That(viewModel.RouteName).IsEqualTo(string.Empty);
        await Assert.That(viewModel.BackendHost).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     The view model updates the route status when the engine raises StatusChanged for a known route.
    /// </summary>
    [Test]
    public async Task EngineStatusChanged_KnownRoute_UpdatesStatusViaScheduler()
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
        var added = viewModel.Routes[0];

        engine.RaiseStatusChanged(added.Identifier, ReverseProxyRouteStatus.Unhealthy);

        await Assert.That(added.Status).IsEqualTo(ReverseProxyRouteStatus.Unhealthy);
    }

    /// <summary>
    ///     The view model ignores StatusChanged events for routes it does not know about.
    /// </summary>
    [Test]
    public async Task EngineStatusChanged_UnknownRoute_IsIgnored()
    {
        var registry = new ReverseProxyRouteRegistry();
        var engine = new StubReverseProxyEngine();
        var viewModel = new ReverseProxySettingsViewModel(registry, engine, InlineUserInterfaceScheduler.Instance);

        engine.RaiseStatusChanged("nope", ReverseProxyRouteStatus.Unhealthy);

        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Disposing the view model unsubscribes from the engine so later events do not update routes.
    /// </summary>
    [Test]
    public async Task Dispose_AfterStatusSubscription_UnsubscribesFromEngine()
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
        var added = viewModel.Routes[0];
        var statusBeforeDispose = added.Status;

        viewModel.Dispose();
        engine.RaiseStatusChanged(added.Identifier, ReverseProxyRouteStatus.Unhealthy);

        await Assert.That(added.Status).IsEqualTo(statusBeforeDispose);
    }

    /// <summary>
    ///     Removing the route currently under edit also clears the editor state.
    /// </summary>
    [Test]
    public async Task RemoveRouteCommand_RouteUnderEdit_ResetsEditor()
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
        var added = viewModel.Routes[0];
        viewModel.EditRouteCommand.Execute(added);

        viewModel.RemoveRouteCommand.Execute(added);

        await Assert.That(viewModel.EditingIdentifier).IsNull();
        await Assert.That(viewModel.Routes.Count).IsEqualTo(0);
    }
}
