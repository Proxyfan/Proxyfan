using Proxyfan.Plugin.Abstractions;
using System;
using System.Reflection;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Static helpers that perform the heavy lifting of plugin instantiation. Kept
///     separate from <see cref="IsolatedPluginInstanceFactory" /> so the factory class
///     can hold only the orchestration entry point while each phase
///     (load context creation, assembly loading, type resolution, activation) lives in
///     a small, independently-testable static method.
/// </summary>
public static class PluginInstanceCreator
{
    /// <summary>
    ///     Activates an instance of <paramref name="entryType" /> in the supplied context.
    /// </summary>
    /// <param name="context">The owning load context.</param>
    /// <param name="entryType">The resolved entry type.</param>
    /// <param name="manifest">The plugin manifest (used for diagnostics).</param>
    /// <returns>An instantiation result.</returns>
    public static PluginInstantiationResult ActivatePlugin(PluginLoadContext context, Type entryType, PluginManifest manifest)
    {
        try
        {
            var instance = Activator.CreateInstance(entryType);
            if (instance is not IProxyfanPlugin plugin)
            {
                PluginLoadContextUnloader.Unload(context);
                return PluginInstantiationResults.Failure($"Activator.CreateInstance returned null or wrong type for '{manifest.EntryTypeName}'.");
            }

            return PluginInstantiationResults.Success(plugin, context);
        }
        catch (Exception ex)
        {
            PluginLoadContextUnloader.Unload(context);
            return PluginInstantiationResults.Failure($"Failed to instantiate plugin: {ex.Message}");
        }
    }

    /// <summary>
    ///     Creates the dedicated load context for the supplied assembly.
    /// </summary>
    /// <param name="assemblyPath">The absolute path of the plugin assembly.</param>
    /// <returns>A success result wrapping the new context or a failure with an error message.</returns>
    public static PluginContextResult CreateContext(string assemblyPath)
    {
        try
        {
            var newContext = new PluginLoadContext(assemblyPath);
            return PluginContextResults.Success(newContext);
        }
        catch (Exception ex)
        {
            return PluginContextResults.Failure($"Failed to create load context: {ex.Message}");
        }
    }

    /// <summary>
    ///     Loads the assembly, resolves the entry type, and activates the plugin.
    /// </summary>
    /// <param name="context">The owning load context.</param>
    /// <param name="assemblyPath">The absolute path of the plugin assembly.</param>
    /// <param name="manifest">The plugin manifest.</param>
    /// <returns>An instantiation result.</returns>
    public static PluginInstantiationResult InstantiateFromContext(PluginLoadContext context, string assemblyPath, PluginManifest manifest)
    {
        Assembly assembly;
        try
        {
            assembly = context.LoadFromAssemblyPath(assemblyPath);
        }
        catch (Exception ex)
        {
            PluginLoadContextUnloader.Unload(context);
            return PluginInstantiationResults.Failure($"Failed to load assembly: {ex.Message}");
        }

        var entryType = assembly.GetType(manifest.EntryTypeName, throwOnError: false, ignoreCase: false);
        if (entryType is null)
        {
            PluginLoadContextUnloader.Unload(context);
            return PluginInstantiationResults.Failure($"Entry type '{manifest.EntryTypeName}' not found in '{manifest.AssemblyFileName}'.");
        }

        if (!typeof(IProxyfanPlugin).IsAssignableFrom(entryType))
        {
            PluginLoadContextUnloader.Unload(context);
            return PluginInstantiationResults.Failure($"Entry type '{manifest.EntryTypeName}' does not implement IProxyfanPlugin.");
        }

        return ActivatePlugin(context, entryType, manifest);
    }
}
