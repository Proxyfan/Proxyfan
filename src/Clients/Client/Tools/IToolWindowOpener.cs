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

    /// <summary>
    ///     Opens the Breakpoint tool window. Idempotent: if the window is already open,
    ///     it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenBreakpoint();

    /// <summary>
    ///     Opens the Certificate Manager tool window. Idempotent: if the window is already
    ///     open, it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenCertificateManager();

    /// <summary>
    ///     Opens the Request Composer tool window. Idempotent: if the window is already
    ///     open, it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenComposer();

    /// <summary>
    ///     Opens the Map Local tool window. Idempotent: if the window is already open,
    ///     it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenMapLocal();

    /// <summary>
    ///     Opens the Map Remote tool window. Idempotent: if the window is already open,
    ///     it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenMapRemote();

    /// <summary>
    ///     Opens the Scripting tool window. Idempotent: if the window is already open,
    ///     it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenScripting();

    /// <summary>
    ///     Opens the SSL Proxying tool window. Idempotent: if the window is already open,
    ///     it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenSecureSocketsLayerProxying();

    /// <summary>
    ///     Opens the Theme picker tool window. Idempotent: if the window is already open,
    ///     it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenTheme();

    /// <summary>
    ///     Opens the Throttle tool window. Idempotent: if the window is already open,
    ///     it is brought to the foreground instead of being recreated.
    /// </summary>
    void OpenThrottle();
}
