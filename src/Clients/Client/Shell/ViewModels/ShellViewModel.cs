using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Proxyfan.Client.Tools;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using Proxyfan.Presentation.Files;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Shell.ViewModels;

/// <summary>
///     View model for the application shell window, managing the system proxy
///     lifecycle controls and application exit.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private const string DefaultSessionFileName = "proxyfan-session.har";
    private const string HarExtension = "har";
    private const string HarTypeDescription = "HTTP Archive (HAR) file";
    private const string OpenSessionTitle = "Open HAR Session";
    private const string SaveSessionTitle = "Save Captured Session as HAR";
    private readonly IFilePickerService _filePicker;
    private readonly IHarExporter _harExporter;
    private readonly IHarImporter _harImporter;
    private readonly IOptionsMonitor<ProxyOptions> _optionsMonitor;
    private readonly ISystemProxy _systemProxy;
    private readonly IToolWindowOpener _toolWindowOpener;
    [ObservableProperty]
    private bool _isSystemProxyEnabled;

    /// <summary>
    ///     Gets the source list view model exposed for the left panel.
    /// </summary>
    public SourceListViewModel SourceList { get; }

    /// <summary>
    ///     Gets the traffic list view model exposed for direct toolbar/status binding.
    /// </summary>
    public TrafficListViewModel TrafficList { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShellViewModel" /> class.
    /// </summary>
    /// <param name="systemProxy">
    ///     The system proxy registration service used to register or unregister the proxy.
    /// </param>
    /// <param name="optionsMonitor">
    ///     Monitor for proxy configuration options, providing the bound port for registration.
    /// </param>
    /// <param name="trafficListViewModel">
    ///     The traffic list view model exposed for toolbar/status bar bindings.
    /// </param>
    /// <param name="sourceListViewModel">
    ///     The source list view model exposed for the left navigation panel.
    /// </param>
    /// <param name="filePicker">The file picker abstraction used to prompt the user for HAR paths.</param>
    /// <param name="harExporter">The HAR exporter used when saving a session.</param>
    /// <param name="harImporter">The HAR importer used when opening a session.</param>
    /// <param name="toolWindowOpener">The opener used to display tool windows on user request.</param>
    public ShellViewModel(
        ISystemProxy systemProxy,
        IOptionsMonitor<ProxyOptions> optionsMonitor,
        TrafficListViewModel trafficListViewModel,
        SourceListViewModel sourceListViewModel,
        IFilePickerService filePicker,
        IHarExporter harExporter,
        IHarImporter harImporter,
        IToolWindowOpener toolWindowOpener)
    {
        _systemProxy = systemProxy;
        _optionsMonitor = optionsMonitor;
        _filePicker = filePicker;
        _harExporter = harExporter;
        _harImporter = harImporter;
        _toolWindowOpener = toolWindowOpener;
        _isSystemProxyEnabled = false;
        TrafficList = trafficListViewModel;
        SourceList = sourceListViewModel;
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
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

    [RelayCommand]
    private async Task SaveSessionAsync(CancellationToken cancellationToken)
    {
        var snapshot = new List<TrafficFlow>(TrafficList.Flows.Count);
        foreach (var viewModel in TrafficList.Flows)
        {
            snapshot.Add(viewModel.Source);
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