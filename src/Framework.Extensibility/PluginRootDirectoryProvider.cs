using System;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Holder for the absolute path of the plugins root directory (e.g.
///     <c>%LOCALAPPDATA%\Proxyfan\plugins\</c>). Resolved during DI registration and
///     consumed by the plugin manager UI for "Open Plugin Folder" actions and by the
///     <see cref="PluginLoader" /> via <see cref="PluginLoader.LoadAll" />.
/// </summary>
public sealed class PluginRootDirectoryProvider
{
    private readonly string _rootDirectory;

    /// <summary>
    ///     Initializes a new <see cref="PluginRootDirectoryProvider" />.
    /// </summary>
    /// <param name="rootDirectory">The absolute path of the plugins root.</param>
    public PluginRootDirectoryProvider(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = rootDirectory;
    }

    /// <summary>
    ///     Gets the absolute path of the plugins root directory.
    /// </summary>
    /// <returns>The absolute path of the plugins root directory.</returns>
    public string GetRootDirectory()
    {
        return _rootDirectory;
    }
}
