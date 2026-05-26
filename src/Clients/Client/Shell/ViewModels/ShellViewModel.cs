using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Proxyfan.Domain.Proxy;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Shell.ViewModels;

/// <summary>
///     View model for the application shell window, managing the system proxy
///     lifecycle controls and application exit.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IOptionsMonitor<ProxyOptions> _optionsMonitor;
    private readonly ISystemProxy _systemProxy;
    [ObservableProperty]
    private bool _isSystemProxyEnabled;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShellViewModel" /> class.
    /// </summary>
    /// <param name="systemProxy">
    ///     The system proxy registration service used to register or unregister the proxy.
    /// </param>
    /// <param name="optionsMonitor">
    ///     Monitor for proxy configuration options, providing the bound port for registration.
    /// </param>
    public ShellViewModel(ISystemProxy systemProxy, IOptionsMonitor<ProxyOptions> optionsMonitor)
    {
        _systemProxy = systemProxy;
        _optionsMonitor = optionsMonitor;
        _isSystemProxyEnabled = false;
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private async Task ToggleSystemProxyAsync(CancellationToken cancellationToken)
    {
        if (IsSystemProxyEnabled)
        {
            await _systemProxy.UnregisterAsync(cancellationToken).ConfigureAwait(false);
            IsSystemProxyEnabled = false;
        }
        else
        {
            var port = _optionsMonitor.CurrentValue.Port;
            await _systemProxy.RegisterAsync(port, cancellationToken).ConfigureAwait(false);
            IsSystemProxyEnabled = true;
        }
    }
}