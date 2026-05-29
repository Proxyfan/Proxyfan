using System;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Static helper that safely unloads a <see cref="PluginLoadContext" />, swallowing
///     the <see cref="InvalidOperationException" /> that is thrown when the context is
///     not collectible or is already being unloaded. Used by the isolated factory to
///     clean up after instantiation failures without masking the original error.
/// </summary>
public static class PluginLoadContextUnloader
{
    /// <summary>
    ///     Attempts to unload <paramref name="context" />. Exceptions are suppressed.
    /// </summary>
    /// <param name="context">The context to unload.</param>
    public static void Unload(PluginLoadContext context)
    {
        try
        {
            context.Unload();
        }
        catch (InvalidOperationException)
        {
            _ = context;
        }
    }
}
