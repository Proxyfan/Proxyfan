using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Default <see cref="PluginFolderLauncherDelegate" /> implementation that spawns
///     <c>explorer.exe</c> to open the supplied directory. Extracted from
///     <see cref="WindowsPluginFolderOpener" /> so the analyzer's
///     static-in-non-static-class rule (ATXCS011) is satisfied.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ExplorerPluginFolderLauncher
{
    /// <summary>
    ///     Launches <c>explorer.exe</c> with the supplied directory path quoted.
    /// </summary>
    /// <param name="directoryPath">The directory to open in Explorer.</param>
    /// <returns>The spawned <see cref="Process" /> or <see langword="null" />.</returns>
    public static IDisposable? Launch(string directoryPath)
    {
        var info = new ProcessStartInfo("explorer.exe", $"\"{directoryPath}\"")
        {
            UseShellExecute = false,
        };
        return Process.Start(info);
    }
}
