namespace Proxyfan.Client.Tools;

/// <summary>
///     Abstraction over the UI-thread operation of showing a tool window. Injected into
///     view models so they can request a tool window to be displayed without holding a
///     hard dependency on a specific window type or the Avalonia application object.
/// </summary>
public interface IToolWindowOpener
{
    /// <summary>
    ///     Opens the Allow List tool window. Idempotent: if the window is already open,
    ///     it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenAllowList();

    /// <summary>
    ///     Opens the Block List tool window. Idempotent: if the window is already open,
    ///     it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenBlockList();
}
