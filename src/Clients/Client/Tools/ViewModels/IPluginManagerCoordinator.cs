using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Presentation-facing boundary for plugin management operations. Hides the
///     framework-level plugin services (<see cref="IPluginRegistry" />,
///     <see cref="IPluginActivationService" />, <see cref="IPluginUpdateFeed" />,
///     <see cref="IPluginHost" />, and <see cref="IPluginDirectoryWatcher" />) behind
///     a single Client-layer contract that <see cref="PluginManagerViewModel" /> can bind to.
/// </summary>
public interface IPluginManagerCoordinator : IDisposable
{
    /// <summary>
    ///     Raised when the plugins directory changes. Listeners run on a background
    ///     thread; marshal to the UI thread as needed.
    /// </summary>
    event PluginsDirectoryChangedHandler? PluginsDirectoryChanged;

    /// <summary>
    ///     Gets a snapshot of currently registered plugins.
    /// </summary>
    IReadOnlyList<IPluginLoadState> Plugins { get; }

    /// <summary>
    ///     Fetches available plugin updates. Returns <see langword="null" /> when the
    ///     feed is not configured, unreachable, or returns an unparseable payload.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of available updates, or <see langword="null" /> on failure.</returns>
    Task<IReadOnlyList<PluginUpdateAvailability>?> FetchUpdatesAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Reloads plugins from the configured plugin root.
    /// </summary>
    void Reload();

    /// <summary>
    ///     Starts watching the plugins directory for changes.
    /// </summary>
    void Start();
}
