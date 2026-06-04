using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Proxyfan.Client.Tools;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Updates;
using Proxyfan.Presentation.Files;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Shell.ViewModels;

/// <summary>
///     View model for the application shell window, managing the system proxy
///     lifecycle controls and application exit.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject, IDisposable
{
    private const string DefaultSessionFileName = "proxyfan-session.har";
    private const string HarExtension = "har";
    private const string HarTypeDescription = "HTTP Archive (HAR) file";
    private const string OpenSessionTitle = "Open HAR Session";
    private const string SaveSessionTitle = "Save Captured Session as HAR";
    private readonly MutableBreakpointConfiguration _breakpointConfiguration;
    private readonly IBreakpointPauseInbox _breakpointPauseInbox;
    private readonly IFilePickerService _filePicker;
    private readonly IHarExporter _harExporter;
    private readonly IHarImporter _harImporter;
    private readonly MutableNoCachingRule _noCachingRule;
    private readonly IOptionsMonitor<ProxyOptions> _optionsMonitor;
    private readonly ISystemProxy _systemProxy;
    private readonly IToolWindowOpener _toolWindowOpener;
    private readonly MutableUpdateNotification _updateNotification;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private int _breakpointPendingPauses;
    [ObservableProperty]
    private bool _isBreakpointEnabled;
    [ObservableProperty]
    private bool _isNoCachingEnabled;
    [ObservableProperty]
    private bool _isRemoteBindingWarningVisible;
    [ObservableProperty]
    private bool _isSystemProxyEnabled;
    [ObservableProperty]
    private bool _isUpdateBannerVisible;
    [ObservableProperty]
    private string? _remoteBindingWarningAddress;
    [ObservableProperty]
    private string? _updateBannerDownloadUrl;
    [ObservableProperty]
    private string? _updateBannerMessage;

    /// <summary>
    ///     Gets the source list view model exposed for the left panel.
    /// </summary>
    public SourceListViewModel SourceList { get; }

    /// <summary>
    ///     Gets the tab host view model that manages the workspace tab strip and tracks
    ///     per-tab filter and selection state.
    /// </summary>
    public TabHostViewModel TabHost { get; }

    /// <summary>
    ///     Gets the traffic list view model exposed for direct toolbar/status binding.
    ///     Shared across all workspace tabs.
    /// </summary>
    public TrafficListViewModel TrafficList => TabHost.TrafficList;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShellViewModel" /> class.
    /// </summary>
    /// <param name="systemProxy">
    ///     The system proxy registration service used to register or unregister the proxy.
    /// </param>
    /// <param name="optionsMonitor">
    ///     Monitor for proxy configuration options, providing the bound port for registration.
    /// </param>
    /// <param name="tabHost">
    ///     The tab host view model that manages workspace tabs and per-tab state.
    /// </param>
    /// <param name="sourceListViewModel">
    ///     The source list view model exposed for the left navigation panel.
    /// </param>
    /// <param name="filePicker">The file picker abstraction used to prompt the user for HAR paths.</param>
    /// <param name="harExporter">The HAR exporter used when saving a session.</param>
    /// <param name="harImporter">The HAR importer used when opening a session.</param>
    /// <param name="toolWindowOpener">The opener used to display tool windows on user request.</param>
    /// <param name="updateNotification">
    ///     Observable update notification that surfaces newly discovered Proxyfan releases.
    /// </param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal banner updates onto the UI thread.</param>
    /// <param name="noCachingRule">Toggleable global No-Caching rule.</param>
    /// <param name="breakpointConfiguration">Toggleable global breakpoint configuration.</param>
    /// <param name="breakpointPauseInbox">Global breakpoint pause inbox used for status-bar queue depth.</param>
    public ShellViewModel(
        ISystemProxy systemProxy,
        IOptionsMonitor<ProxyOptions> optionsMonitor,
        TabHostViewModel tabHost,
        SourceListViewModel sourceListViewModel,
        IFilePickerService filePicker,
        IHarExporter harExporter,
        IHarImporter harImporter,
        IToolWindowOpener toolWindowOpener,
        MutableUpdateNotification updateNotification,
        IUserInterfaceScheduler userInterfaceScheduler,
        MutableNoCachingRule noCachingRule,
        MutableBreakpointConfiguration breakpointConfiguration,
        IBreakpointPauseInbox breakpointPauseInbox)
    {
        _systemProxy = systemProxy;
        _optionsMonitor = optionsMonitor;
        _filePicker = filePicker;
        _harExporter = harExporter;
        _harImporter = harImporter;
        _toolWindowOpener = toolWindowOpener;
        _updateNotification = updateNotification;
        _userInterfaceScheduler = userInterfaceScheduler;
        _noCachingRule = noCachingRule;
        _breakpointConfiguration = breakpointConfiguration;
        _breakpointPauseInbox = breakpointPauseInbox;
        _breakpointPendingPauses = breakpointPauseInbox.PendingCount;
        _isSystemProxyEnabled = false;
        _isUpdateBannerVisible = false;
        _isNoCachingEnabled = noCachingRule.IsEnabled;
        _isBreakpointEnabled = breakpointConfiguration.IsEnabled;
        ConfigureRemoteBindingWarning();
        TabHost = tabHost;
        SourceList = sourceListViewModel;
        _updateNotification.Changed += OnUpdateNotificationChanged;
        _breakpointPauseInbox.PauseAdded += OnBreakpointPauseChanged;
        _breakpointPauseInbox.PauseResolved += OnBreakpointPauseChanged;
        ApplyUpdate(_updateNotification.Latest);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _updateNotification.Changed -= OnUpdateNotificationChanged;
        _breakpointPauseInbox.PauseAdded -= OnBreakpointPauseChanged;
        _breakpointPauseInbox.PauseResolved -= OnBreakpointPauseChanged;
    }

    private void ApplyUpdate(UpdateInfo? update)
    {
        if (update is null)
        {
            IsUpdateBannerVisible = false;
            UpdateBannerMessage = null;
            UpdateBannerDownloadUrl = null;
            return;
        }

        UpdateBannerMessage = $"Proxyfan {update.Version} is available.";
        UpdateBannerDownloadUrl = update.DownloadUrl;
        IsUpdateBannerVisible = true;
    }

    private bool CanShowRemoteBindingWarning(string bindAddress)
    {
        if (!IPAddress.TryParse(bindAddress, out var parsedAddress))
        {
            return false;
        }

        return !IPAddress.IsLoopback(parsedAddress);
    }

    private void ConfigureRemoteBindingWarning()
    {
        var bindAddress = _optionsMonitor.CurrentValue.BindAddress;
        var isRemoteBinding = CanShowRemoteBindingWarning(bindAddress);
        IsRemoteBindingWarningVisible = isRemoteBinding;
        RemoteBindingWarningAddress = isRemoteBinding ? bindAddress : null;
    }

    [RelayCommand]
    private void DismissUpdateBanner()
    {
        IsUpdateBannerVisible = false;
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void OnBreakpointPauseChanged(BreakpointPause pause)
    {
        _ = pause;
        if (_userInterfaceScheduler.HasAccess())
        {
            RefreshBreakpointPendingPauses();
            return;
        }

        _userInterfaceScheduler.Post(RefreshBreakpointPendingPauses);
    }

    private void OnUpdateNotificationChanged(UpdateInfo? update)
    {
        if (_userInterfaceScheduler.HasAccess())
        {
            ApplyUpdate(update);
            return;
        }

        _userInterfaceScheduler.Post(() => ApplyUpdate(update));
    }

    [RelayCommand]
    private void OpenAllowList()
    {
        _toolWindowOpener.OpenAllowList();
    }

    [RelayCommand]
    private void OpenBlockList()
    {
        _toolWindowOpener.OpenBlockList();
    }

    [RelayCommand]
    private void OpenBreakpoint()
    {
        _toolWindowOpener.OpenBreakpoint();
    }

    [RelayCommand]
    private void OpenCertificateManager()
    {
        _toolWindowOpener.OpenCertificateManager();
    }

    [RelayCommand]
    private void OpenComposer()
    {
        _toolWindowOpener.OpenComposer();
    }

    [RelayCommand]
    private void OpenCustomColumns()
    {
        _toolWindowOpener.OpenCustomColumns();
    }

    [RelayCommand]
    private void OpenDiffTool()
    {
        _toolWindowOpener.OpenDiffTool();
    }

    [RelayCommand]
    private void OpenDomainNameSystemSpoofing()
    {
        _toolWindowOpener.OpenDomainNameSystemSpoofing();
    }

    [RelayCommand]
    private void OpenKeyboardShortcuts()
    {
        _toolWindowOpener.OpenKeyboardShortcuts();
    }

    [RelayCommand]
    private void OpenMapLocal()
    {
        _toolWindowOpener.OpenMapLocal();
    }

    [RelayCommand]
    private void OpenMapRemote()
    {
        _toolWindowOpener.OpenMapRemote();
    }

    [RelayCommand]
    private void OpenPluginManager()
    {
        _toolWindowOpener.OpenPluginManager();
    }

    [RelayCommand]
    private void OpenPreferences()
    {
        _toolWindowOpener.OpenPreferences();
    }

    [RelayCommand]
    private void OpenRemoteDevices()
    {
        _toolWindowOpener.OpenRemoteDevices();
    }

    [RelayCommand]
    private void OpenRemoteProcedureCallDescriptors()
    {
        _toolWindowOpener.OpenRemoteProcedureCallDescriptors();
    }

    [RelayCommand]
    private void OpenReverseProxy()
    {
        _toolWindowOpener.OpenReverseProxy();
    }

    [RelayCommand]
    private void OpenScripting()
    {
        _toolWindowOpener.OpenScripting();
    }

    [RelayCommand]
    private void OpenSecureSocketsLayerProxying()
    {
        _toolWindowOpener.OpenSecureSocketsLayerProxying();
    }

    [RelayCommand]
    private async Task OpenSessionAsync(CancellationToken cancellationToken)
    {
        var request = new FilePickerOpenRequest
        {
            Title = OpenSessionTitle,
            FileExtension = HarExtension,
            ExtensionDescription = HarTypeDescription,
        };
        var stream = await _filePicker.OpenForReadAsync(request, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return;
        }

        try
        {
            var imported = await _harImporter.ImportAsync(stream, cancellationToken).ConfigureAwait(false);
            TrafficList.LoadFlows(imported);
        }
        finally
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private void OpenTheme()
    {
        _toolWindowOpener.OpenTheme();
    }

    [RelayCommand]
    private void OpenThrottle()
    {
        _toolWindowOpener.OpenThrottle();
    }

    private void RefreshBreakpointPendingPauses()
    {
        BreakpointPendingPauses = _breakpointPauseInbox.PendingCount;
    }

    [RelayCommand]
    private async Task SaveSessionAsync(CancellationToken cancellationToken)
    {
        var snapshot = new List<TrafficFlow>(TrafficList.Flows.Count);
        foreach (var viewModel in TrafficList.Flows)
        {
            snapshot.Add(viewModel.GetDomainFlow());
        }

        var request = new FilePickerSaveRequest
        {
            Title = SaveSessionTitle,
            DefaultFileName = DefaultSessionFileName,
            FileExtension = HarExtension,
            ExtensionDescription = HarTypeDescription,
        };
        var stream = await _filePicker.OpenForWriteAsync(request, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return;
        }

        try
        {
            await _harExporter.ExportAsync(snapshot, stream, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private void ToggleBreakpoint()
    {
        var newValue = !_breakpointConfiguration.IsEnabled;
        _breakpointConfiguration.SetEnabled(newValue);
        IsBreakpointEnabled = newValue;
    }

    [RelayCommand]
    private void ToggleNoCaching()
    {
        var newValue = !_noCachingRule.IsEnabled;
        _noCachingRule.SetEnabled(newValue);
        IsNoCachingEnabled = newValue;
    }

    [RelayCommand]
    private async Task ToggleSystemProxyAsync(CancellationToken cancellationToken)
    {
        if (IsSystemProxyEnabled)
        {
            await _systemProxy.UnregisterAsync(cancellationToken).ConfigureAwait(false);
            IsSystemProxyEnabled = false;
        }
        else
        {
            var port = _optionsMonitor.CurrentValue.Port;
            await _systemProxy.RegisterAsync(port, cancellationToken).ConfigureAwait(false);
            IsSystemProxyEnabled = true;
        }
    }
}