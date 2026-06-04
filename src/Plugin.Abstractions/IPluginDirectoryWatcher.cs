using System;

namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     Abstraction over a filesystem watcher for the plugins root. Implementations raise
///     <see cref="PluginsDirectoryChanged" /> whenever a subdirectory is added, removed, or
///     renamed, allowing the plugin manager UI to surface a "directory changed — reload to
///     pick up changes" affordance.
/// </summary>
public interface IPluginDirectoryWatcher : IDisposable
{
    /// <summary>
    ///     Raised whenever the watched directory changes. Listeners run on a background
    ///     thread; marshal to the UI thread as needed.
    /// </summary>
    event PluginsDirectoryChangedHandler? PluginsDirectoryChanged;

    /// <summary>
    ///     Starts watching the configured directory. Safe to call multiple times; the watcher
    ///     is idempotent and only attaches once.
    /// </summary>
    void Start();
}
