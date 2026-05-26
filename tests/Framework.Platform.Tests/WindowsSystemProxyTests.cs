using Microsoft.Win32;
using Proxyfan.Framework.Platform;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Integration tests for <see cref="WindowsSystemProxy" />.
///     Covers registry updates while restoring the caller's original proxy configuration.
/// </summary>
[NotInParallel]
public sealed class WindowsSystemProxyTests
{
    private const string ProxyEnableValueName = "ProxyEnable";
    private const string ProxyServerValueName = "ProxyServer";
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    /// <summary>
    ///     Verifies that registering the system proxy enables proxying for the provided port.
    ///     Also verifies that the registry server endpoint is written in loopback form.
    /// </summary>
    [Test]
    public async Task RegisterAsync_WhenCalledWithPort_SetsRegistryValues(CancellationToken cancellationToken)
    {
        var proxy = new WindowsSystemProxy();
        var originalState = CaptureProxyRegistryState();

        try
        {
            await proxy.RegisterAsync(9090, cancellationToken);
            var enable = ReadProxyEnable();
            var server = ReadProxyServer();

            await Assert.That(enable).IsEqualTo(1);
            await Assert.That(server).IsEqualTo("127.0.0.1:9090");
        }
        finally
        {
            RestoreProxyRegistryState(originalState);
        }
    }

    /// <summary>
    ///     Verifies that unregistering the system proxy disables proxying after registration.
    ///     Also verifies that the proxy server value is removed.
    /// </summary>
    [Test]
    public async Task UnregisterAsync_WhenCalledAfterRegister_DisablesProxy(CancellationToken cancellationToken)
    {
        var proxy = new WindowsSystemProxy();
        var originalState = CaptureProxyRegistryState();

        try
        {
            await proxy.RegisterAsync(9091, cancellationToken);
            await proxy.UnregisterAsync(cancellationToken);
            var enable = ReadProxyEnable();
            var server = ReadProxyServer();

            await Assert.That(enable).IsEqualTo(0);
            await Assert.That(server).IsNull();
        }
        finally
        {
            RestoreProxyRegistryState(originalState);
        }
    }

    private static ProxyRegistryState CaptureProxyRegistryState()
    {
        using var key = OpenInternetSettingsKey();
        var isProxyEnablePresent = HasValue(key, ProxyEnableValueName);
        var proxyEnable = ReadProxyEnable(key) ?? 0;
        var isProxyServerPresent = HasValue(key, ProxyServerValueName);
        var proxyServer = ReadProxyServer(key);
        var state = new ProxyRegistryState(isProxyEnablePresent, proxyEnable, isProxyServerPresent, proxyServer);
        return state;
    }

    private static bool HasValue(RegistryKey key, string valueName)
    {
        return Array.Exists(key.GetValueNames(), currentValueName => currentValueName == valueName);
    }

    private static RegistryKey OpenInternetSettingsKey()
    {
        RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);

        if (key is not null)
        {
            return key;
        }

        return Registry.CurrentUser.CreateSubKey(RegistryPath, true);
    }

    private static int? ReadProxyEnable()
    {
        using var key = OpenInternetSettingsKey();
        return ReadProxyEnable(key);
    }

    private static int? ReadProxyEnable(RegistryKey key)
    {
        var rawValue = key.GetValue(ProxyEnableValueName);

        if (rawValue is int proxyEnable)
        {
            return proxyEnable;
        }

        return null;
    }

    private static string? ReadProxyServer()
    {
        using var key = OpenInternetSettingsKey();
        return ReadProxyServer(key);
    }

    private static string? ReadProxyServer(RegistryKey key)
    {
        var rawValue = key.GetValue(ProxyServerValueName);

        if (rawValue is string proxyServer)
        {
            return proxyServer;
        }

        return null;
    }

    private static void RestoreProxyRegistryState(ProxyRegistryState state)
    {
        using var key = OpenInternetSettingsKey();

        if (state.IsProxyEnablePresent)
        {
            key.SetValue(ProxyEnableValueName, state.ProxyEnable, RegistryValueKind.DWord);
        }
        else
        {
            key.DeleteValue(ProxyEnableValueName, false);
        }

        if (state.IsProxyServerPresent && state.ProxyServer is string proxyServer)
        {
            key.SetValue(ProxyServerValueName, proxyServer, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ProxyServerValueName, false);
        }
    }

    private sealed class ProxyRegistryState
    {
        public ProxyRegistryState(
            bool isProxyEnablePresent,
            int proxyEnable,
            bool isProxyServerPresent,
            string? proxyServer)
        {
            IsProxyEnablePresent = isProxyEnablePresent;
            ProxyEnable = proxyEnable;
            IsProxyServerPresent = isProxyServerPresent;
            ProxyServer = proxyServer;
        }

        public bool IsProxyEnablePresent { get; }

        public int ProxyEnable { get; }

        public bool IsProxyServerPresent { get; }

        public string? ProxyServer { get; }
    }
}