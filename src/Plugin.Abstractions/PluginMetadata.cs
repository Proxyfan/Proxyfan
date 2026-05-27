namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     Metadata that describes a Proxyfan plugin to the host. Plugins return an instance of
///     this record via <see cref="IProxyfanPlugin.Metadata" />.
/// </summary>
public sealed class PluginMetadata
{
    /// <summary>
    ///     Gets the plugin's required host API version. Major-version mismatch with the host
    ///     causes the plugin to be flagged as incompatible.
    /// </summary>
    public string ApiVersion { get; }

    /// <summary>
    ///     Gets the human-readable author name.
    /// </summary>
    public string Author { get; }

    /// <summary>
    ///     Gets the long-form plugin description shown in the manager UI.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Gets the unique plugin identifier (reverse-DNS recommended, e.g.
    ///     <c>"com.example.json-formatter"</c>).
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Gets the human-readable plugin name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the plugin version (SemVer recommended).
    /// </summary>
    public string Version { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginMetadata" />.
    /// </summary>
    /// <param name="id">The unique plugin identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="version">The plugin version.</param>
    /// <param name="author">The author name.</param>
    /// <param name="description">The long-form description.</param>
    /// <param name="apiVersion">The required host API version (SemVer).</param>
    public PluginMetadata(string id, string name, string version, string author, string description, string apiVersion)
    {
        Id = id;
        Name = name;
        Version = version;
        Author = author;
        Description = description;
        ApiVersion = apiVersion;
    }
}
