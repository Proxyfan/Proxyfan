using Proxyfan.Plugin.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Default implementation of <see cref="IPluginManagerCoordinator" /> that wraps
///     <see cref="IPluginRegistry" />, <see cref="IPluginActivationService" />,
///     <see cref="IPluginUpdateFeed" />, <see cref="IPluginHost" />, and
///     <see cref="IPluginDirectoryWatcher" /> behind the presentation boundary.
/// </summary>
public sealed class PluginManagerCoordinator : IPluginManagerCoordinator
{
    /// <inheritdoc />
    public event PluginsDirectoryChangedHandler? PluginsDirectoryChanged;

    private readonly IPluginActivationService _activationService;
    private readonly IPluginDirectoryWatcher _directoryWatcher;
    private readonly IPluginHost _pluginHost;
    private readonly IPluginRegistry _registry;
    private readonly IPluginUpdateFeed _updateFeed;

    /// <summary>
    ///     Initializes a new <see cref="PluginManagerCoordinator" />.
    /// </summary>
    /// <param name="registry">The plugin registry to expose.</param>
    /// <param name="activationService">The activation service used for reload flows.</param>
    /// <param name="updateFeed">The remote update feed consulted by update checks.</param>
    /// <param name="pluginHost">The plugin host providing the API version for compatibility checks.</param>
    /// <param name="directoryWatcher">The plugin directory watcher.</param>
    public PluginManagerCoordinator(
        IPluginRegistry registry,
        IPluginActivationService activationService,
        IPluginUpdateFeed updateFeed,
        IPluginHost pluginHost,
        IPluginDirectoryWatcher directoryWatcher)
    {
        _registry = registry;
        _activationService = activationService;
        _updateFeed = updateFeed;
        _pluginHost = pluginHost;
        _directoryWatcher = directoryWatcher;
        _directoryWatcher.PluginsDirectoryChanged += OnDirectoryChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _directoryWatcher.PluginsDirectoryChanged -= OnDirectoryChanged;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PluginUpdateAvailability>?> FetchUpdatesAsync(CancellationToken cancellationToken)
    {
        var manifest = await _updateFeed.FetchAsync(cancellationToken).ConfigureAwait(false);
        if (manifest is null)
        {
            return null;
        }

        return [.. PluginUpdateAvailabilityResolver.Resolve(_registry, manifest, _pluginHost.ApiVersion)];
    }

    /// <inheritdoc />
    public IReadOnlyList<IPluginLoadState> Plugins => _registry.Plugins;

    /// <inheritdoc />
    public void Reload()
    {
        _activationService.Reload();
    }

    /// <inheritdoc />
    public void Start()
    {
        _directoryWatcher.Start();
    }

    private void OnDirectoryChanged()
    {
        PluginsDirectoryChanged?.Invoke();
    }
}
