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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "proxyfan-watcher-" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }
}
