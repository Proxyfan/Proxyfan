using Microsoft.Win32;
using Proxyfan.Domain.Proxy;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Updates the current-user Windows system proxy settings in the registry and
///     broadcasts the change to WinINet so running clients refresh their cached
///     proxy configuration. Without the broadcast, applications that had already
///     resolved the system proxy keep using the previous value until they restart.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSystemProxy : ISystemProxy
{
    private const string ProxyEnableValueName = "ProxyEnable";
    private const string ProxyServerValueName = "ProxyServer";
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    private readonly InternetSettingsRefreshDelegate _refresh;

    /// <summary>
    ///     Initializes a new <see cref="WindowsSystemProxy" /> that refreshes WinINet's
    ///     cached proxy settings via <see cref="WindowsInternetSettingsRefresher.Refresh" />
    ///     after each registry update.
    /// </summary>
    public WindowsSystemProxy()
        : this(WindowsInternetSettingsRefresher.Refresh)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="WindowsSystemProxy" /> with a custom internet
    ///     settings refresh delegate. Intended for unit testing; production callers
    ///     should use the parameterless constructor that calls WinINet.
    /// </summary>
    /// <param name="refresh">
    ///     A callback invoked after each successful registry update to notify the OS
    ///     and already-running WinINet clients that the proxy configuration changed.
    /// </param>
    public WindowsSystemProxy(InternetSettingsRefreshDelegate refresh)
    {
        _refresh = refresh;
    }

    /// <inheritdoc />
    public Task RegisterAsync(int port, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var key = OpenRegistryKey();
        key.SetValue(ProxyEnableValueName, 1, RegistryValueKind.DWord);
        key.SetValue(ProxyServerValueName, $"127.0.0.1:{port}", RegistryValueKind.String);
        _refresh();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var key = OpenRegistryKey();
        key.SetValue(ProxyEnableValueName, 0, RegistryValueKind.DWord);
        key.DeleteValue(ProxyServerValueName, false);
        _refresh();
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
