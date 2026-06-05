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
///     via <see cref="IPluginManagerCoordinator" />, drives reload flows, surfaces remote
///     update availability via Check for Updates, and tracks plugins-directory changes so
///     that hot-added plugin folders are detected without manual refresh. Whether any user
///     action requires a process restart for the change to fully take effect (assembly load
///     contexts are sticky) is tracked via <see cref="IsRestartRequired" />.
/// </summary>
public sealed partial class PluginManagerViewModel : ObservableObject, IDisposable
{
    private readonly IPluginManagerCoordinator _coordinator;
    private readonly IPluginEnabledStateStore _enabledStateStore;
    private readonly IPluginFolderOpener _folderOpener;
    private readonly Lock _lifecycleLock;
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
    ///     coordinator, enabled-state store, folder opener, and UI scheduler.
    /// </summary>
    /// <param name="coordinator">The coordinator that wraps plugin registry, activation, update, and directory-watching services.</param>
    /// <param name="enabledStateStore">The store used by rows to read + persist the user's enable choice.</param>
    /// <param name="folderOpener">The folder opener used by rows for Open Folder commands.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal bound-state updates onto the UI thread.</param>
    public PluginManagerViewModel(
        IPluginManagerCoordinator coordinator,
        IPluginEnabledStateStore enabledStateStore,
        IPluginFolderOpener folderOpener,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _coordinator = coordinator;
        _enabledStateStore = enabledStateStore;
        _folderOpener = folderOpener;
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
        _coordinator.PluginsDirectoryChanged += OnDirectoryChanged;
        _coordinator.Start();
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
            _coordinator.PluginsDirectoryChanged -= OnDirectoryChanged;
            _coordinator.Dispose();
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
            var availabilities = await _coordinator.FetchUpdatesAsync(cancellationToken).ConfigureAwait(true);
            if (availabilities is null)
            {
                IsUpdateCheckFailed = true;
                UpdateCheckStatus = "Update check failed or feed not configured.";
                AvailableUpdates.Clear();
                IsAnyUpdateAvailable = false;
                return;
            }

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
        foreach (var plugin in _coordinator.Plugins)
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
        _coordinator.Reload();
        IsRestartRequired = true;
        RefreshSnapshot();
    }
}
