using Proxyfan.Plugin.Abstractions;
using System.Runtime.Loader;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Outcome of <see cref="IPluginInstanceFactory.Create" />. Either <see cref="Plugin" />
///     is non-null (success, with the optional owning <see cref="LoadContext" /> for later
///     unload) or <see cref="ErrorMessage" /> describes why instantiation failed.
/// </summary>
public sealed class PluginInstantiationResult
{
    /// <summary>
    ///     Gets a human-readable error message when <see cref="IsSuccess" /> is false.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     Gets a value indicating whether instantiation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the assembly load context owning <see cref="Plugin" />, when the factory
    ///     supports later unload. May be null on success when the factory does not isolate
    ///     (e.g. test stub).
    /// </summary>
    public AssemblyLoadContext? LoadContext { get; }

    /// <summary>
    ///     Gets the live plugin instance on success, otherwise null.
    /// </summary>
    public IProxyfanPlugin? Plugin { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginInstantiationResult" />. Use
    ///     <see cref="PluginInstantiationResults.Success" /> or
    ///     <see cref="PluginInstantiationResults.Failure" /> for typical construction.
    /// </summary>
    /// <param name="plugin">The plugin instance on success.</param>
    /// <param name="loadContext">The owning load context on success (optional).</param>
    /// <param name="errorMessage">The failure message.</param>
    /// <param name="isSuccess">Whether the instantiation succeeded.</param>
    public PluginInstantiationResult(IProxyfanPlugin? plugin, AssemblyLoadContext? loadContext, string? errorMessage, bool isSuccess)
    {
        Plugin = plugin;
        LoadContext = loadContext;
        ErrorMessage = errorMessage;
        IsSuccess = isSuccess;
    }
}
