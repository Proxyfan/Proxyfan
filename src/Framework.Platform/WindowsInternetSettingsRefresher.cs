using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Default <see cref="InternetSettingsRefreshDelegate" /> implementation that asks
///     the Windows WinINet stack to refresh its cached per-user proxy settings by calling
///     <c>InternetSetOption</c> with <c>INTERNET_OPTION_SETTINGS_CHANGED</c> followed by
///     <c>INTERNET_OPTION_REFRESH</c>. Without this broadcast, already-running clients
///     (Internet Explorer, Edge, Office, .NET <c>HttpClient</c> using the system proxy,
///     and many other WinINet consumers) continue using the previous proxy configuration
///     until they restart. Extracted from <see cref="WindowsSystemProxy" /> so the
///     analyzer's static-in-non-static-class rule (ATXCS011) is satisfied and so the
///     P/Invoke can be swapped out in unit tests.
/// </summary>
[SupportedOSPlatform("windows")]
public static partial class WindowsInternetSettingsRefresher
{
    private const int InternetOptionRefresh = 37;
    private const int InternetOptionSettingsChanged = 39;

    /// <summary>
    ///     Notifies WinINet that the per-user proxy settings have changed and forces it
    ///     to re-read them. Throws <see cref="Win32Exception" /> if either notification
    ///     fails so the caller can surface the failure rather than silently leaving
    ///     running clients on stale proxy settings.
    /// </summary>
    public static void Refresh()
    {
        if (!CanSetInternetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!CanSetInternetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    [LibraryImport("wininet.dll", EntryPoint = "InternetSetOptionW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CanSetInternetOption(
        IntPtr internetHandle,
        int optionIdentifier,
        IntPtr bufferPointer,
        int bufferLength);
}
