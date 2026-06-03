using System.Runtime.Versioning;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Abstraction over the WinINet refresh APIs that notify already-running
///     applications of a change to the per-user Windows Internet Settings (for
///     example, the proxy configuration). Without this broadcast, processes that
///     have cached the proxy configuration continue using the previous values
///     until they restart.
/// </summary>
[SupportedOSPlatform("windows")]
public interface IWindowsInternetSettingsRefresher
{
    /// <summary>
    ///     Notifies the operating system that the per-user Internet Settings
    ///     have changed and asks WinINet to reload them so that subsequent
    ///     connections observe the new configuration.
    /// </summary>
    /// <exception cref="System.ComponentModel.Win32Exception">
    ///     Thrown when one of the underlying WinINet refresh calls fails.
    /// </exception>
    void Refresh();
}
