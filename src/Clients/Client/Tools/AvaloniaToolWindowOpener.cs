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
}
