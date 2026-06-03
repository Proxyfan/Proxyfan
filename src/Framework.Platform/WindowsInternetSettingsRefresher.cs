using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Default <see cref="IWindowsInternetSettingsRefresher" /> implementation
///     that calls into <c>wininet.dll</c> to broadcast a settings change and
///     refresh the proxy configuration for already-running processes.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsInternetSettingsRefresher : IWindowsInternetSettingsRefresher
{
    /// <inheritdoc />
    public void Refresh()
    {
        InvokeInternetSetOption(WindowsInternetSettingsRefresherNativeMethods.InternetOptionSettingsChanged);
        InvokeInternetSetOption(WindowsInternetSettingsRefresherNativeMethods.InternetOptionRefresh);
    }

    private void InvokeInternetSetOption(int option)
    {
        var resultCode = WindowsInternetSettingsRefresherNativeMethods.BroadcastInternetSetOption(option);

        if (resultCode != 0)
        {
            return;
        }

        var errorCode = Marshal.GetLastWin32Error();
        throw new Win32Exception(
            errorCode,
            $"InternetSetOption({option}) failed while broadcasting the Windows proxy settings change.");
    }
}
