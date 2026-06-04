namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     Read-only state for a loaded (or failed) plugin entry.
/// </summary>
public interface IPluginLoadState
{
    /// <summary>
    ///     Gets the failure message when <see cref="IsLoaded" /> is false, otherwise null.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    ///     Gets a value indicating whether the plugin loaded successfully.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    ///     Gets the plugin metadata.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    ///     Gets the plugin source directory path when available.
    /// </summary>
    string? SourceDirectory { get; }
}
