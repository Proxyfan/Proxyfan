using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Helpers for showing tool windows. Centralizes the logic for choosing whether to
///     show a window owned by the main shell or as a top-level window.
/// </summary>
public static class ToolWindowDisplay
{
    /// <summary>
    ///     Shows the supplied window. If the application has a classic desktop lifetime with
    ///     a main window, the new window is shown as a child of the main window; otherwise it
    ///     is shown as a top-level window.
    /// </summary>
    /// <param name="window">The window to show.</param>
    public static void Show(Window window)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } mainWindow)
        {
            window.Show(mainWindow);
            return;
        }

        window.Show();
    }
}
