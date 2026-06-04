namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     Contract for activating and reloading plugins.
/// </summary>
public interface IPluginActivationService
{
    /// <summary>
    ///     Gets a value indicating whether plugins have been activated at least once.
    /// </summary>
    bool IsActivated { get; }

    /// <summary>
    ///     Activates plugins if they are not already loaded.
    /// </summary>
    void EnsureLoaded();

    /// <summary>
    ///     Reloads plugins from the configured plugin root.
    /// </summary>
    void Reload();
}
