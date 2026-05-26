using Microsoft.Win32;
using Proxyfan.Domain.Proxy;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Updates the current-user Windows system proxy settings in the registry.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSystemProxy : ISystemProxy
{
    private const string ProxyEnableValueName = "ProxyEnable";
    private const string ProxyServerValueName = "ProxyServer";
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    /// <inheritdoc />
    public Task RegisterAsync(int port, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var key = OpenRegistryKey();
        key.SetValue(ProxyEnableValueName, 1, RegistryValueKind.DWord);
        key.SetValue(ProxyServerValueName, $"127.0.0.1:{port}", RegistryValueKind.String);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var key = OpenRegistryKey();
        key.SetValue(ProxyEnableValueName, 0, RegistryValueKind.DWord);
        key.DeleteValue(ProxyServerValueName, false);
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