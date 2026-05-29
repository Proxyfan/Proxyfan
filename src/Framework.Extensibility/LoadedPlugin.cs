using Proxyfan.Plugin.Abstractions;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Represents a plugin that has been loaded by <see cref="PluginRegistry" />: its metadata,
///     the live <see cref="IProxyfanPlugin" /> instance, the load result, and an optional
///     error message when load failed.
/// </summary>
public sealed class LoadedPlugin
{
    /// <summary>
    ///     Gets the failure message when <see cref="IsLoaded" /> is false, otherwise null.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     Gets the plugin instance, or null when the plugin failed to load.
    /// </summary>
    public IProxyfanPlugin? Instance { get; }

    /// <summary>
    ///     Gets a value indicating whether the plugin loaded successfully.
    /// </summary>
    public bool IsLoaded { get; }

    /// <summary>
    ///     Gets the plugin metadata (always present, even when load failed).
    /// </summary>
    public PluginMetadata Metadata { get; }

    /// <summary>
    ///     Gets the absolute path of the plugin's source directory on disk, or null when
    ///     the entry did not originate from a discovered directory (e.g. tests, in-process
    ///     loads).
    /// </summary>
    public string? SourceDirectory { get; }

    /// <summary>
    ///     Initializes a new <see cref="LoadedPlugin" />.
    /// </summary>
    /// <param name="metadata">The plugin metadata.</param>
    /// <param name="instance">The instance, or null when load failed.</param>
    /// <param name="isLoaded">Whether the load succeeded.</param>
    /// <param name="errorMessage">The error message, or null on success.</param>
    /// <param name="sourceDirectory">The plugin's source directory on disk, or null.</param>
    public LoadedPlugin(PluginMetadata metadata, IProxyfanPlugin? instance, bool isLoaded, string? errorMessage, string? sourceDirectory)
    {
        Metadata = metadata;
        Instance = instance;
        IsLoaded = isLoaded;
        ErrorMessage = errorMessage;
        SourceDirectory = sourceDirectory;
    }
}
