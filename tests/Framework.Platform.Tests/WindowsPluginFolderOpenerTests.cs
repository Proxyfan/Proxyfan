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
}
