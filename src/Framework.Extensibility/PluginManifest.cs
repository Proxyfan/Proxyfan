using Proxyfan.Plugin.Abstractions;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Strongly-typed view of a parsed <c>plugin.manifest</c> file. Describes the
///     plugin's metadata, the assembly file relative to the plugin directory, and the
///     fully-qualified entry type name that implements <see cref="IProxyfanPlugin" />.
/// </summary>
public sealed class PluginManifest
{
    /// <summary>
    ///     Gets the relative path (within the plugin directory) of the assembly to load.
    /// </summary>
    public string AssemblyFileName { get; }

    /// <summary>
    ///     Gets the fully-qualified type name (including namespace) implementing
    ///     <see cref="IProxyfanPlugin" /> that the loader should instantiate.
    /// </summary>
    public string EntryTypeName { get; }

    /// <summary>
    ///     Gets the plugin metadata that the loader will surface to the registry and UI.
    /// </summary>
    public PluginMetadata Metadata { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginManifest" />.
    /// </summary>
    /// <param name="metadata">The plugin metadata.</param>
    /// <param name="assemblyFileName">The assembly file name (relative path).</param>
    /// <param name="entryTypeName">The fully-qualified entry type name.</param>
    public PluginManifest(PluginMetadata metadata, string assemblyFileName, string entryTypeName)
    {
        Metadata = metadata;
        AssemblyFileName = assemblyFileName;
        EntryTypeName = entryTypeName;
    }
}
