using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     P/Invoke surface for the WinINet APIs used to broadcast a change to the
///     per-user Internet Settings. Lives in its own static class so the
///     analyzer's static-in-non-static-class rule (ATXCS011) is satisfied and
///     so a managed wrapper can keep the native entry point private (S4200).
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class WindowsInternetSettingsRefresherNativeMethods
{
    /// <summary>
    ///     Asks WinINet to reload the proxy configuration from the registry.
    /// </summary>
    public const int InternetOptionRefresh = 37;

    /// <summary>
    ///     Notifies WinINet that the Internet Settings have changed so that
    ///     handles created after the call observe the new configuration.
    /// </summary>
    public const int InternetOptionSettingsChanged = 39;

    /// <summary>
    ///     Managed wrapper around <c>InternetSetOptionW</c> that calls the API
    ///     without a value buffer (the form used for the
    ///     <see cref="InternetOptionSettingsChanged" /> and
    ///     <see cref="InternetOptionRefresh" /> broadcasts).
    /// </summary>
    /// <param name="option">The WinINet option identifier.</param>
    /// <returns>
    ///     The raw <c>BOOL</c> result from the underlying call — non-zero on
    ///     success, zero on failure. Callers can read
    ///     <see cref="Marshal.GetLastWin32Error" /> for the failure code.
    /// </returns>
    public static int BroadcastInternetSetOption(int option)
    {
        return InternetSetOption(IntPtr.Zero, option, IntPtr.Zero, 0);
    }

    [LibraryImport("wininet.dll", EntryPoint = "InternetSetOptionW", SetLastError = true)]
    private static partial int InternetSetOption(
        IntPtr internetHandle,
        int option,
        IntPtr buffer,
        int bufferLength);
}
