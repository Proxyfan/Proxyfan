namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     A flat, immutable projection of an <see cref="IPluginLoadState" /> that carries
///     all fields required to build a plugin row in the UI. Created from an
///     <see cref="IPluginLoadState" /> at the application-wiring boundary so that
///     Presentation-layer code does not need to reference Framework implementation types.
/// </summary>
public sealed class PluginStateSnapshot
{
    /// <summary>
    ///     Gets the plugin's required host API version.
    /// </summary>
    public string ApiVersion { get; }

    /// <summary>
    ///     Gets the human-readable author name.
    /// </summary>
    public string Author { get; }

    /// <summary>
    ///     Gets the long-form plugin description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Gets the failure message when <see cref="IsLoaded" /> is false, otherwise null.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     Gets the unique plugin identifier.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///     Gets a value indicating whether the plugin loaded successfully.
    /// </summary>
    public bool IsLoaded { get; }

    /// <summary>
    ///     Gets the human-readable plugin name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the absolute path of the plugin's source directory on disk, or null when
    ///     the entry did not originate from a discovered directory.
    /// </summary>
    public string? SourceDirectory { get; }

    /// <summary>
    ///     Gets the plugin version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginStateSnapshot" />.
    /// </summary>
    /// <param name="identifier">The unique plugin identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="version">The plugin version.</param>
    /// <param name="author">The author name.</param>
    /// <param name="description">The long-form description.</param>
    /// <param name="apiVersion">The required host API version.</param>
    /// <param name="isLoaded">Whether the plugin loaded successfully.</param>
    /// <param name="errorMessage">The error message, or null on success.</param>
    /// <param name="sourceDirectory">The plugin's source directory on disk, or null.</param>
    public PluginStateSnapshot(
        string identifier,
        string name,
        string version,
        string author,
        string description,
        string apiVersion,
        bool isLoaded,
        string? errorMessage,
        string? sourceDirectory)
    {
        Identifier = identifier;
        Name = name;
        Version = version;
        Author = author;
        Description = description;
        ApiVersion = apiVersion;
        IsLoaded = isLoaded;
        ErrorMessage = errorMessage;
        SourceDirectory = sourceDirectory;
    }
}
