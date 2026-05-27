using Microsoft.Extensions.DependencyInjection;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Client.Tools.Views;
using System;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Avalonia-backed <see cref="IToolWindowOpener" /> implementation. Resolves view models
///     from the DI container, instantiates the matching window, and shows it. If a window
///     of the requested type is already open, it is brought to the foreground.
/// </summary>
public sealed class AvaloniaToolWindowOpener : IToolWindowOpener
{
    private readonly IServiceProvider _serviceProvider;
    private AllowListWindow? _allowListWindow;
    private BlockListWindow? _blockListWindow;
    private BreakpointWindow? _breakpointWindow;
    private CertificateManagerWindow? _certificateManagerWindow;
    private MapLocalWindow? _mapLocalWindow;
    private MapRemoteWindow? _mapRemoteWindow;
    private ScriptingWindow? _scriptingWindow;
    private SecureSocketsLayerProxyingWindow? _secureSocketsLayerProxyingWindow;
    private ThemeWindow? _themeWindow;
    private ThrottleWindow? _throttleWindow;

    /// <summary>
    ///     Initializes a new <see cref="AvaloniaToolWindowOpener" />.
    /// </summary>
    /// <param name="serviceProvider">The DI container used to resolve tool view models.</param>
    public AvaloniaToolWindowOpener(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public void OpenAllowList()
    {
        if (_allowListWindow is not null)
        {
            _allowListWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<AllowListViewModel>();
        var window = new AllowListWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _allowListWindow = null;
        };
        _allowListWindow = window;
        ToolWindowDisplay.Show(window);
    }

    /// <inheritdoc />
    public void OpenBlockList()
    {
        if (_blockListWindow is not null)
        {
            _blockListWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<BlockListViewModel>();
        var window = new BlockListWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _blockListWindow = null;
        };
        _blockListWindow = window;
        ToolWindowDisplay.Show(window);
    }

    /// <inheritdoc />
    public void OpenBreakpoint()
    {
        if (_breakpointWindow is not null)
        {
            _breakpointWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<BreakpointViewModel>();
        var window = new BreakpointWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _breakpointWindow = null;
        };
        _breakpointWindow = window;
        ToolWindowDisplay.Show(window);
    }

    /// <inheritdoc />
    public void OpenCertificateManager()
    {
        if (_certificateManagerWindow is not null)
        {
            _certificateManagerWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<CertificateManagerViewModel>();
        var window = new CertificateManagerWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _certificateManagerWindow = null;
        };
        _certificateManagerWindow = window;
        ToolWindowDisplay.Show(window);
    }

    /// <inheritdoc />
    public void OpenMapLocal()
    {
        if (_mapLocalWindow is not null)
        {
            _mapLocalWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<MapLocalViewModel>();
        var window = new MapLocalWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _mapLocalWindow = null;
        };
        _mapLocalWindow = window;
        ToolWindowDisplay.Show(window);
    }

    /// <inheritdoc />
    public void OpenMapRemote()
    {
        if (_mapRemoteWindow is not null)
        {
            _mapRemoteWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<MapRemoteViewModel>();
        var window = new MapRemoteWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _mapRemoteWindow = null;
        };
        _mapRemoteWindow = window;
        ToolWindowDisplay.Show(window);
    }

    /// <inheritdoc />
    public void OpenScripting()
    {
        if (_scriptingWindow is not null)
        {
            _scriptingWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<ScriptingViewModel>();
        var window = new ScriptingWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _scriptingWindow = null;
        };
        _scriptingWindow = window;
        ToolWindowDisplay.Show(window);
    }

    /// <inheritdoc />
    public void OpenSecureSocketsLayerProxying()
    {
        if (_secureSocketsLayerProxyingWindow is not null)
        {
            _secureSocketsLayerProxyingWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<SecureSocketsLayerProxyingViewModel>();
        var window = new SecureSocketsLayerProxyingWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _secureSocketsLayerProxyingWindow = null;
        };
        _secureSocketsLayerProxyingWindow = window;
        ToolWindowDisplay.Show(window);
    }

    /// <inheritdoc />
    public void OpenTheme()
    {
        if (_themeWindow is not null)
        {
            _themeWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<ThemeViewModel>();
        var window = new ThemeWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _themeWindow = null;
        };
        _themeWindow = window;
        ToolWindowDisplay.Show(window);
    }

    /// <inheritdoc />
    public void OpenThrottle()
    {
        if (_throttleWindow is not null)
        {
            _throttleWindow.Activate();
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<ThrottleViewModel>();
        var window = new ThrottleWindow
        {
            DataContext = viewModel,
        };
        window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _throttleWindow = null;
        };
        _throttleWindow = window;
        ToolWindowDisplay.Show(window);
    }
}
