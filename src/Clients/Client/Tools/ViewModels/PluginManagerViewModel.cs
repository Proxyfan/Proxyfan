using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Plugin.Abstractions;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Plugin Manager tool window. Exposes the list of loaded plugins
///     surfaced by <see cref="IPluginRegistry" />, drives <see cref="IPluginActivationService" />
///     for reload flows, surfaces remote update availability via <see cref="IPluginUpdateFeed" />,
///     and tracks plugins-directory changes via <see cref="IPluginDirectoryWatcher" /> so
///     that hot-added plugin folders are detected without manual refresh. Whether any user
///     action requires a process restart for the change to fully take effect (assembly load
///     contexts are sticky) is tracked via <see cref="IsRestartRequired" />.
/// </summary>
public sealed partial class PluginManagerViewModel : ObservableObject, IDisposable
{
    private readonly IPluginActivationService _activationService;
    private readonly IPluginDirectoryWatcher _directoryWatcher;
    private readonly IPluginEnabledStateStore _enabledStateStore;
    private readonly IPluginFolderOpener _folderOpener;
    private readonly Lock _lifecycleLock;
    private readonly IPluginHost _pluginHost;
    private readonly IPluginRegistry _registry;
    private readonly IPluginUpdateFeed _updateFeed;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private bool _isAnyUpdateAvailable;
    [ObservableProperty]
    private bool _isCheckingForUpdates;
    private bool _isDisposed;
    [ObservableProperty]
    private bool _isRestartRequired;
    [ObservableProperty]
    private bool _isUpdateCheckFailed;
    [ObservableProperty]
    private string _summary;
    [ObservableProperty]
    private string _updateCheckStatus;

    /// <summary>
    ///     Gets the observable collection of available plugin updates surfaced by the most
    ///     recent <see cref="CheckForUpdatesCommand" /> invocation.
    /// </summary>
    public ObservableCollection<PluginUpdateAvailabilityViewModel> AvailableUpdates { get; }

    /// <summary>
    ///     Gets the observable collection of plugin rows currently displayed.
    /// </summary>
    public ObservableCollection<PluginItemViewModel> Plugins { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginManagerViewModel" /> bound to the supplied
    ///     plugin registry, enabled-state store, folder opener, activation service, update
    ///     feed, plugin host, and directory watcher.
    /// </summary>
    /// <param name="registry">The plugin registry to expose.</param>
    /// <param name="enabledStateStore">The store used by rows to read + persist the user's enable choice.</param>
    /// <param name="folderOpener">The folder opener used by rows for Open Folder commands.</param>
    /// <param name="activationService">The activation service used by the Reload command.</param>
    /// <param name="updateFeed">The remote update feed consulted by Check for Updates.</param>
    /// <param name="pluginHost">The plugin host (used for API version compatibility checks against advertised updates).</param>
    /// <param name="directoryWatcher">The plugin directory watcher that fires when subdirectories are added/removed.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal bound-state updates onto the UI thread.</param>
    public PluginManagerViewModel(
        IPluginRegistry registry,
        IPluginEnabledStateStore enabledStateStore,
        IPluginFolderOpener folderOpener,
        IPluginActivationService activationService,
        IPluginUpdateFeed updateFeed,
        IPluginHost pluginHost,
        IPluginDirectoryWatcher directoryWatcher,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _registry = registry;
        _enabledStateStore = enabledStateStore;
        _folderOpener = folderOpener;
        _activationService = activationService;
        _updateFeed = updateFeed;
        _pluginHost = pluginHost;
        _directoryWatcher = directoryWatcher;
        var lifecycleLock = new Lock();
        _lifecycleLock = lifecycleLock;
        _userInterfaceScheduler = userInterfaceScheduler;
        _summary = string.Empty;
        _updateCheckStatus = string.Empty;
        _isRestartRequired = false;
        _isCheckingForUpdates = false;
        _isUpdateCheckFailed = false;
        Plugins = [];
        AvailableUpdates = [];
        RefreshSnapshot();
        _directoryWatcher.PluginsDirectoryChanged += OnDirectoryChanged;
        _directoryWatcher.Start();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _directoryWatcher.PluginsDirectoryChanged -= OnDirectoryChanged;
        }
    }

    private void ApplyDirectoryChangedUpdate()
    {
        lock (_lifecycleLock)
        {
            if (_isDisposed)
            {
                return;
            }

            IsRestartRequired = true;
            UpdateCheckStatus = "Plugins folder changed — reload to pick up changes.";
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        IsCheckingForUpdates = true;
        IsUpdateCheckFailed = false;
        UpdateCheckStatus = "Checking for updates…";
        try
        {
            var manifest = await _updateFeed.FetchAsync(cancellationToken).ConfigureAwait(true);
            if (manifest is null)
            {
                IsUpdateCheckFailed = true;
                UpdateCheckStatus = "Update check failed or feed not configured.";
                AvailableUpdates.Clear();
                IsAnyUpdateAvailable = false;
                return;
            }

            var availabilities = PluginUpdateAvailabilityResolver.Resolve(_registry, manifest, _pluginHost.ApiVersion);
            AvailableUpdates.Clear();
            foreach (var availability in availabilities)
            {
                var availabilityModel = new PluginUpdateAvailabilityModel(
                    availability.Identifier,
                    availability.Name,
                    availability.Author,
                    availability.CurrentVersion,
                    availability.LatestVersion,
                    availability.DownloadUrl,
                    availability.IsCompatible);
                var availabilityViewModel = new PluginUpdateAvailabilityViewModel(availabilityModel);
                AvailableUpdates.Add(availabilityViewModel);
            }

            IsAnyUpdateAvailable = AvailableUpdates.Count > 0;
            if (AvailableUpdates.Count == 0)
            {
                UpdateCheckStatus = "All plugins are up to date.";
            }
            else
            {
                UpdateCheckStatus = $"{AvailableUpdates.Count} update(s) available.";
            }
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private void OnDirectoryChanged()
    {
        lock (_lifecycleLock)
        {
            if (_isDisposed)
            {
                return;
            }
        }

        _userInterfaceScheduler.Post(ApplyDirectoryChangedUpdate);
    }

    private void OnPluginStateChanged()
    {
        IsRestartRequired = true;
        RefreshSnapshot();
    }

    [RelayCommand]
    private void Refresh()
    {
        RefreshSnapshot();
    }

    private void RefreshSnapshot()
    {
        Plugins.Clear();
        var totalCount = 0;
        var failedCount = 0;
        foreach (var plugin in _registry.Plugins)
        {
            var snapshot = new PluginStateSnapshot(
                plugin.Metadata.Id,
                plugin.Metadata.Name,
                plugin.Metadata.Version,
                plugin.Metadata.Author,
                plugin.Metadata.Description,
                plugin.Metadata.ApiVersion,
                plugin.IsLoaded,
                plugin.ErrorMessage,
                plugin.SourceDirectory);
            var viewModel = new PluginItemViewModel(snapshot, _enabledStateStore, _folderOpener, OnPluginStateChanged);
            Plugins.Add(viewModel);
            totalCount++;
            if (!plugin.IsLoaded)
            {
                failedCount++;
            }
        }

        var loadedCount = totalCount - failedCount;
        Summary = $"{totalCount} plugin(s) registered — {loadedCount} loaded, {failedCount} failed.";
    }

    [RelayCommand]
    private void Reload()
    {
        _activationService.Reload();
        IsRestartRequired = true;
        RefreshSnapshot();
    }
}
