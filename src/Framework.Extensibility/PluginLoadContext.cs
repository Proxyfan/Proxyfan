using System;
using System.Reflection;
using System.Runtime.Loader;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     <see cref="AssemblyLoadContext" /> dedicated to a single plugin. Resolves the
///     plugin's primary assembly and any private dependencies that live alongside it in
///     the plugin directory, while delegating well-known framework and abstraction
///     assemblies (e.g. <c>Proxyfan.Plugin.Abstractions</c>) to the default load context so
///     that types remain identity-equal across the host/plugin boundary.
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    ///     Gets the absolute path of the plugin's primary assembly.
    /// </summary>
    public string AssemblyPath { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginLoadContext" /> for the supplied assembly.
    /// </summary>
    /// <param name="assemblyPath">The absolute path of the primary assembly.</param>
    public PluginLoadContext(string assemblyPath)
        : base(name: PluginLoadContextNaming.Build(assemblyPath), isCollectible: true)
    {
        var resolver = new AssemblyDependencyResolver(assemblyPath);
        AssemblyPath = assemblyPath;
        _resolver = resolver;
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (PluginSharedContractAssemblies.HasMatch(assemblyName))
        {
            return null;
        }

        var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (resolvedPath is not null)
        {
            var assembly = LoadFromAssemblyPath(resolvedPath);
            return assembly;
        }

        return null;
    }

    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var resolvedPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (resolvedPath is not null)
        {
            var handle = LoadUnmanagedDllFromPath(resolvedPath);
            return handle;
        }

        return IntPtr.Zero;
    }
}
