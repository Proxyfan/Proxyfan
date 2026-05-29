using Proxyfan.Framework.Extensibility;
using System;
using System.IO;
using System.Threading;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     <see cref="IPluginDirectoryWatcher" /> implementation that wraps a
///     <see cref="FileSystemWatcher" /> rooted at the configured plugins directory. Filters
///     for directory-level events (subdirectory created, deleted, or renamed) so we don't
///     fire on transient inner-file activity (e.g. plugin writing log files). The first
///     event in a burst is coalesced into a single notification using a short debounce
///     window — Windows raises multiple events per logical action.
/// </summary>
public sealed class FileSystemPluginDirectoryWatcher : IPluginDirectoryWatcher
{
    /// <inheritdoc />
    public event PluginsDirectoryChangedHandler? PluginsDirectoryChanged;

    private const int DebounceMilliseconds = 250;
    private readonly Lock _lock;
    private readonly PluginRootDirectoryProvider _rootProvider;
    private bool _isDisposed;
    private bool _isStarted;
    private long _lastNotificationTicks;
    private FileSystemWatcher? _watcher;

    /// <summary>
    ///     Initializes a new <see cref="FileSystemPluginDirectoryWatcher" />.
    /// </summary>
    /// <param name="rootProvider">The plugins root directory provider.</param>
    public FileSystemPluginDirectoryWatcher(PluginRootDirectoryProvider rootProvider)
    {
        var newLock = new Lock();
        _rootProvider = rootProvider;
        _lock = newLock;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            if (_watcher is not null)
            {
                _watcher.Created -= OnDirectoryEvent;
                _watcher.Deleted -= OnDirectoryEvent;
                _watcher.Renamed -= OnDirectoryRenamed;
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (_lock)
        {
            if (_isStarted || _isDisposed)
            {
                return;
            }

            var rootDirectory = _rootProvider.GetRootDirectory();
            if (!Directory.Exists(rootDirectory))
            {
                try
                {
                    Directory.CreateDirectory(rootDirectory);
                }
                catch (Exception ex)
                {
                    _ = ex;
                    return;
                }
            }

            try
            {
                var watcher = new FileSystemWatcher(rootDirectory)
                {
                    NotifyFilter = NotifyFilters.DirectoryName,
                    IncludeSubdirectories = false,
                };
                watcher.Created += OnDirectoryEvent;
                watcher.Deleted += OnDirectoryEvent;
                watcher.Renamed += OnDirectoryRenamed;
                watcher.EnableRaisingEvents = true;
                _watcher = watcher;
                _isStarted = true;
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }

    private void OnDirectoryEvent(object sender, FileSystemEventArgs eventArgs)
    {
        RaiseDebounced();
    }

    private void OnDirectoryRenamed(object sender, RenamedEventArgs eventArgs)
    {
        RaiseDebounced();
    }

    private void RaiseDebounced()
    {
        var now = DateTime.UtcNow.Ticks;
        var previous = Interlocked.Read(ref _lastNotificationTicks);
        var elapsed = TimeSpan.FromTicks(now - previous);
        if (elapsed.TotalMilliseconds < DebounceMilliseconds)
        {
            return;
        }

        Interlocked.Exchange(ref _lastNotificationTicks, now);
        PluginsDirectoryChanged?.Invoke();
    }
}
