using Microsoft.Win32;
using Proxyfan.Domain.Proxy;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Updates the current-user Windows system proxy settings in the registry
///     and notifies already-running processes of the change via WinINet.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSystemProxy : ISystemProxy
{
    private const string ProxyEnableValueName = "ProxyEnable";
    private const string ProxyServerValueName = "ProxyServer";
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    private readonly IWindowsInternetSettingsRefresher _internetSettingsRefresher;

    /// <summary>
    ///     Initializes a new instance of <see cref="WindowsSystemProxy" />.
    /// </summary>
    /// <param name="internetSettingsRefresher">
    ///     Adapter used to broadcast the Internet Settings change to WinINet so
    ///     already-running applications pick up the new proxy configuration.
    /// </param>
    public WindowsSystemProxy(IWindowsInternetSettingsRefresher internetSettingsRefresher)
    {
        _internetSettingsRefresher = internetSettingsRefresher;
    }

    /// <inheritdoc />
    public Task RegisterAsync(int port, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (var key = OpenRegistryKey())
        {
            key.SetValue(ProxyEnableValueName, 1, RegistryValueKind.DWord);
            key.SetValue(ProxyServerValueName, $"127.0.0.1:{port}", RegistryValueKind.String);
        }

        _internetSettingsRefresher.Refresh();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (var key = OpenRegistryKey())
        {
            key.SetValue(ProxyEnableValueName, 0, RegistryValueKind.DWord);
            key.DeleteValue(ProxyServerValueName, false);
        }

        _internetSettingsRefresher.Refresh();
        return Task.CompletedTask;
    }

    private RegistryKey OpenRegistryKey()
    {
        RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);

        if (key is not null)
        {
            return key;
        }

        return Registry.CurrentUser.CreateSubKey(RegistryPath, true);
    }
}
