using Proxyfan.Framework.Extensibility;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Tests for <see cref="FileSystemPluginDirectoryWatcher" />.
/// </summary>
[NotInParallel]
public sealed class FileSystemPluginDirectoryWatcherTests
{
    /// <summary>
    ///     Disposing without starting is a no-op.
    /// </summary>
    [Test]
    public async Task Dispose_WithoutStart_NoThrow()
    {
        var rootDirectory = CreateTempDirectory();
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            var watcher = new FileSystemPluginDirectoryWatcher(provider);

            watcher.Dispose();

            await Assert.That(Directory.Exists(rootDirectory)).IsTrue();
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    /// <summary>
    ///     Disposing twice is a no-op.
    /// </summary>
    [Test]
    public async Task Dispose_CalledTwice_NoThrow()
    {
        var rootDirectory = CreateTempDirectory();
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            var watcher = new FileSystemPluginDirectoryWatcher(provider);
            watcher.Start();
            watcher.Dispose();
            watcher.Dispose();

            await Assert.That(Directory.Exists(rootDirectory)).IsTrue();
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    /// <summary>
    ///     Start succeeds on a missing root directory by creating it.
    /// </summary>
    [Test]
    public async Task Start_MissingRoot_CreatesDirectory()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "proxyfan-watcher-" + Path.GetRandomFileName());
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            using var watcher = new FileSystemPluginDirectoryWatcher(provider);

            watcher.Start();

            await Assert.That(Directory.Exists(rootDirectory)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    ///     Start called twice is idempotent (second call is a no-op).
    /// </summary>
    [Test]
    public async Task Start_CalledTwice_NoThrow()
    {
        var rootDirectory = CreateTempDirectory();
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            using var watcher = new FileSystemPluginDirectoryWatcher(provider);

            watcher.Start();
            watcher.Start();

            await Assert.That(Directory.Exists(rootDirectory)).IsTrue();
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    /// <summary>
    ///     Creating a subdirectory triggers the change event.
    /// </summary>
    [Test]
    public async Task CreateSubdirectory_AfterStart_FiresEvent()
    {
        var rootDirectory = CreateTempDirectory();
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            using var watcher = new FileSystemPluginDirectoryWatcher(provider);
            var signal = new TaskCompletionSource();
            watcher.PluginsDirectoryChanged += () => signal.TrySetResult();
            watcher.Start();

            Directory.CreateDirectory(Path.Combine(rootDirectory, "new-plugin"));
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cancellation.Token.Register(() => signal.TrySetCanceled());
            try
            {
                await signal.Task;
                await Assert.That(signal.Task.IsCompletedSuccessfully).IsTrue();
            }
            catch (TaskCanceledException)
            {
                await Assert.That(signal.Task.IsCompletedSuccessfully).IsTrue();
            }
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    /// <summary>
    ///     Renaming a subdirectory triggers the change event (covers OnDirectoryRenamed).
    /// </summary>
    [Test]
    public async Task RenameSubdirectory_AfterStart_FiresEvent()
    {
        var rootDirectory = CreateTempDirectory();
        var originalPath = Path.Combine(rootDirectory, "before-rename");
        Directory.CreateDirectory(originalPath);
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            using var watcher = new FileSystemPluginDirectoryWatcher(provider);
            var signal = new TaskCompletionSource();
            watcher.PluginsDirectoryChanged += () => signal.TrySetResult();
            watcher.Start();

            Directory.Move(originalPath, Path.Combine(rootDirectory, "after-rename"));
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cancellation.Token.Register(() => signal.TrySetCanceled());
            try
            {
                await signal.Task;
                await Assert.That(signal.Task.IsCompletedSuccessfully).IsTrue();
            }
            catch (TaskCanceledException)
            {
                await Assert.That(signal.Task.IsCompletedSuccessfully).IsTrue();
            }
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    /// <summary>
    ///     Deleting a subdirectory triggers the change event (covers OnDirectoryEvent for Deleted).
    /// </summary>
    [Test]
    public async Task DeleteSubdirectory_AfterStart_FiresEvent()
    {
        var rootDirectory = CreateTempDirectory();
        var pluginDirectory = Path.Combine(rootDirectory, "to-delete");
        Directory.CreateDirectory(pluginDirectory);
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            using var watcher = new FileSystemPluginDirectoryWatcher(provider);
            var signal = new TaskCompletionSource();
            watcher.PluginsDirectoryChanged += () => signal.TrySetResult();
            watcher.Start();

            Directory.Delete(pluginDirectory);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cancellation.Token.Register(() => signal.TrySetCanceled());
            try
            {
                await signal.Task;
                await Assert.That(signal.Task.IsCompletedSuccessfully).IsTrue();
            }
            catch (TaskCanceledException)
            {
                await Assert.That(signal.Task.IsCompletedSuccessfully).IsTrue();
            }
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    ///     Verifies that Start, Dispose, then Start again does not restart the watcher
    ///     (Start is idempotent across dispose; Start checks the disposed flag).
    /// </summary>
    [Test]
    public async Task Start_AfterDispose_IsNoOp()
    {
        var rootDirectory = CreateTempDirectory();
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            var watcher = new FileSystemPluginDirectoryWatcher(provider);
            watcher.Start();
            watcher.Dispose();

            watcher.Start();
            var raised = false;
            watcher.PluginsDirectoryChanged += () => raised = true;
            Directory.CreateDirectory(Path.Combine(rootDirectory, "after-dispose"));
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            await Task.Yield();
            await Assert.That(raised).IsFalse();
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    /// <summary>
    ///     Simulates a folder-copy scenario: a subdirectory is created, then plugin files
    ///     are written into it in a burst. The trailing-edge debounce must coalesce the
    ///     burst into a single notification that fires after the burst settles — and never
    ///     before the inner files exist, which is the regression this test guards against.
    /// </summary>
    [Test]
    public async Task FolderCopy_WithDelayedInnerFiles_FiresAfterBurstSettles()
    {
        var rootDirectory = CreateTempDirectory();
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            using var watcher = new FileSystemPluginDirectoryWatcher(provider);
            var signal = new TaskCompletionSource<DateTime>();
            watcher.PluginsDirectoryChanged += () => signal.TrySetResult(DateTime.UtcNow);
            watcher.Start();

            var pluginDirectory = Path.Combine(rootDirectory, "incoming-plugin");
            Directory.CreateDirectory(pluginDirectory);
            // Stagger inner-file writes so the burst extends past the initial directory
            // creation; without trailing-edge debounce + IncludeSubdirectories the
            // notification would fire on the bare directory and then never retry.
            for (var i = 0; i < 5; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), TimeProvider.System, CancellationToken.None);
                await File.WriteAllTextAsync(Path.Combine(pluginDirectory, $"file-{i}.bin"), "payload");
            }

            var lastWriteUtc = DateTime.UtcNow;
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cancellation.Token.Register(() => signal.TrySetCanceled());
            var firedAtUtc = await signal.Task;

            await Assert.That(firedAtUtc).IsGreaterThanOrEqualTo(lastWriteUtc);
            await Assert.That(Directory.Exists(pluginDirectory)).IsTrue();
            await Assert.That(File.Exists(Path.Combine(pluginDirectory, "file-4.bin"))).IsTrue();
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    /// <summary>
    ///     A burst of directory events must coalesce into a single notification rather than
    ///     firing once per event.
    /// </summary>
    [Test]
    public async Task RapidBurst_AfterStart_CoalescesIntoSingleNotification()
    {
        var rootDirectory = CreateTempDirectory();
        try
        {
            var provider = new PluginRootDirectoryProvider(rootDirectory);
            using var watcher = new FileSystemPluginDirectoryWatcher(provider);
            var notificationCount = 0;
            watcher.PluginsDirectoryChanged += () => Interlocked.Increment(ref notificationCount);
            watcher.Start();

            for (var i = 0; i < 5; i++)
            {
                Directory.CreateDirectory(Path.Combine(rootDirectory, $"plugin-{i}"));
            }

            // Wait long enough for the trailing-edge debounce (250 ms) to settle plus margin.
            await Task.Delay(TimeSpan.FromSeconds(2), TimeProvider.System, CancellationToken.None);

            await Assert.That(Volatile.Read(ref notificationCount)).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-watcher-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }
}
