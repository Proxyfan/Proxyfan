using Proxyfan.Plugin.Abstractions;
using System.Runtime.Loader;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Factory helpers for constructing <see cref="PluginInstantiationResult" /> instances.
/// </summary>
public static class PluginInstantiationResults
{
    /// <summary>
    ///     Constructs a failure result.
    /// </summary>
    /// <param name="errorMessage">The error description.</param>
    /// <returns>The failure.</returns>
    public static PluginInstantiationResult Failure(string errorMessage)
    {
        var result = new PluginInstantiationResult(null, null, errorMessage, false);
        return result;
    }

    /// <summary>
    ///     Constructs a success result.
    /// </summary>
    /// <param name="plugin">The live plugin.</param>
    /// <param name="loadContext">The owning load context (optional).</param>
    /// <returns>The success.</returns>
    public static PluginInstantiationResult Success(IProxyfanPlugin plugin, AssemblyLoadContext? loadContext)
    {
        var result = new PluginInstantiationResult(plugin, loadContext, null, true);
        return result;
    }
}
