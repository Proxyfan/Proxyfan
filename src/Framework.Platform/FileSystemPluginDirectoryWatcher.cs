using Proxyfan.Framework.Extensibility;
using Proxyfan.Plugin.Abstractions;
using System;
using System.IO;
using System.Threading;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     <see cref="IPluginDirectoryWatcher" /> implementation that wraps a
///     <see cref="FileSystemWatcher" /> rooted at the configured plugins directory. Watches
///     directory-level events (subdirectory created, deleted, or renamed) plus file-name
///     events inside plugin subdirectories so that an in-progress folder copy/extract
///     eventually triggers a single notification once the burst settles. Steady-state
///     activity (e.g. a plugin appending to its own log file) is intentionally ignored by
///     subscribing only to create/delete/rename — not raw <c>Changed</c> events — so that
///     long-running plugin writes do not keep deferring the notification forever. Events
///     are coalesced via a trailing-edge debounce timer; the notification fires only after
///     <see cref="DebounceMilliseconds" /> have elapsed with no further events, which is
///     what makes the "directory created, then files copied in" sequence observable as one
///     logical change.
/// </summary>
public sealed class FileSystemPluginDirectoryWatcher : IPluginDirectoryWatcher
{
    /// <inheritdoc />
    public event PluginsDirectoryChangedHandler? PluginsDirectoryChanged;

    private const int DebounceMilliseconds = 250;
    private readonly Lock _lock;
    private readonly PluginRootDirectoryProvider _rootProvider;
    private Timer? _debounceTimer;
    private bool _isDisposed;
    private bool _isStarted;
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

            _debounceTimer?.Dispose();
            _debounceTimer = null;
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
                var debounceTimer = new Timer(OnDebounceElapsed, state: null, Timeout.Infinite, Timeout.Infinite);
                _debounceTimer = debounceTimer;
                var watcher = new FileSystemWatcher(rootDirectory)
                {
                    NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName,
                    IncludeSubdirectories = true,
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
                _debounceTimer?.Dispose();
                _debounceTimer = null;
            }
        }
    }

    private void OnDebounceElapsed(object? state)
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }
        }

        PluginsDirectoryChanged?.Invoke();
    }

    private void OnDirectoryEvent(object sender, FileSystemEventArgs eventArgs)
    {
        ScheduleDebounced();
    }

    private void OnDirectoryRenamed(object sender, RenamedEventArgs eventArgs)
    {
        ScheduleDebounced();
    }

    private void ScheduleDebounced()
    {
        lock (_lock)
        {
            if (_isDisposed || _debounceTimer is null)
            {
                return;
            }

            _debounceTimer.Change(DebounceMilliseconds, Timeout.Infinite);
        }
    }
}
