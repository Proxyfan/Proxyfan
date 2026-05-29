using System;
using System.Reflection;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Identifies the assemblies whose types must be loaded in the default
///     <see cref="System.Runtime.Loader.AssemblyLoadContext" /> so that type identity is
///     preserved across the host/plugin boundary. When a plugin asks its own ALC for one
///     of these assemblies, the ALC must return <c>null</c> to delegate the load to the
///     default context.
/// </summary>
public static class PluginSharedContractAssemblies
{
    private const string FullName = "Proxyfan.Plugin.Abstractions";
    private const string ShortName = "Plugin.Abstractions";

    /// <summary>
    ///     Returns <c>true</c> when <paramref name="assemblyName" /> refers to a shared
    ///     contract assembly that must be served from the default load context.
    /// </summary>
    /// <param name="assemblyName">The assembly name resolution request.</param>
    /// <returns>True when shared.</returns>
    public static bool HasMatch(AssemblyName assemblyName)
    {
        if (string.Equals(assemblyName.Name, ShortName, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(assemblyName.Name, FullName, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
