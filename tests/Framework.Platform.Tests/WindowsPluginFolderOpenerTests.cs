using System;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Tests for <see cref="WindowsPluginFolderOpener" />. These tests only verify that
///     the opener's defensive guards (null / whitespace / non-existent path) do not throw;
///     actually launching <c>explorer.exe</c> would spawn a window on the developer's
///     desktop and is therefore not asserted against.
/// </summary>
[NotInParallel]
public sealed class WindowsPluginFolderOpenerTests
{
    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Test]
    public async Task Open_NullPath_DoesNotThrow()
    {
        var opener = new WindowsPluginFolderOpener();

        var thrown = Capture(() => opener.Open(null!));

        await Assert.That(thrown).IsNull();
    }

    [Test]
    public async Task Open_EmptyPath_DoesNotThrow()
    {
        var opener = new WindowsPluginFolderOpener();

        var thrown = Capture(() => opener.Open(string.Empty));

        await Assert.That(thrown).IsNull();
    }

    [Test]
    public async Task Open_WhitespacePath_DoesNotThrow()
    {
        var opener = new WindowsPluginFolderOpener();

        var thrown = Capture(() => opener.Open("   "));

        await Assert.That(thrown).IsNull();
    }

    [Test]
    public async Task Open_NonExistentPath_DoesNotThrow()
    {
        var opener = new WindowsPluginFolderOpener();
        var missing = Path.Combine(Path.GetTempPath(), "proxyfan-folder-opener-missing-" + Path.GetRandomFileName());

        var thrown = Capture(() => opener.Open(missing));

        await Assert.That(thrown).IsNull();
    }

    /// <summary>
    ///     Verifies that with a real directory and a custom launcher, the launcher is invoked
    ///     exactly once with the supplied directory path.
    /// </summary>
    [Test]
    public async Task Open_ExistingDirectoryWithCustomLauncher_InvokesLauncher()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-folder-opener-launch-" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            string? launchedPath = null;
            var opener = new WindowsPluginFolderOpener(path =>
            {
                launchedPath = path;
                return null;
            });

            opener.Open(directory);

            await Assert.That(launchedPath).IsEqualTo(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that an exception thrown by the launcher is swallowed and does not
    ///     propagate to the caller.
    /// </summary>
    [Test]
    public async Task Open_LauncherThrows_SwallowsException()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-folder-opener-throws-" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            var opener = new WindowsPluginFolderOpener(_ => throw new InvalidOperationException("simulated"));

            var thrown = Capture(() => opener.Open(directory));

            await Assert.That(thrown).IsNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    ///     Verifies that a launcher that returns a disposable wrapper has its disposable
    ///     disposed exactly once.
    /// </summary>
    [Test]
    public async Task Open_LauncherReturnsDisposable_DisposesIt()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-folder-opener-dispose-" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            var disposable = new TrackingDisposable();
            var opener = new WindowsPluginFolderOpener(_ => disposable);

            opener.Open(directory);

            await Assert.That(disposable.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class TrackingDisposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
