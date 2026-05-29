using System.IO;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Static helper that derives a deterministic, human-readable name for a
///     <see cref="PluginLoadContext" /> from the plugin assembly path. Names appear in
///     <c>AssemblyLoadContext.All</c> diagnostics and crash dumps, so they should be
///     unique per plugin and contain enough context to identify the source.
/// </summary>
public static class PluginLoadContextNaming
{
    /// <summary>
    ///     Builds the load-context name (e.g. <c>PluginLoadContext(MyPlugin)</c>) from
    ///     the supplied assembly path.
    /// </summary>
    /// <param name="assemblyPath">The absolute path of the plugin's primary assembly.</param>
    /// <returns>The load-context display name.</returns>
    public static string Build(string assemblyPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
        var contextName = $"PluginLoadContext({fileName})";
        return contextName;
    }
}
