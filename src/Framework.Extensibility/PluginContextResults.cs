namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Factory helpers for <see cref="PluginContextResult" />.
/// </summary>
public static class PluginContextResults
{
    /// <summary>
    ///     Builds a failure result.
    /// </summary>
    /// <param name="errorMessage">A description of why context creation failed.</param>
    /// <returns>The failure result.</returns>
    public static PluginContextResult Failure(string errorMessage)
    {
        var result = new PluginContextResult(null, errorMessage, false);
        return result;
    }

    /// <summary>
    ///     Builds a success result.
    /// </summary>
    /// <param name="context">The newly-created load context.</param>
    /// <returns>The success result.</returns>
    public static PluginContextResult Success(PluginLoadContext context)
    {
        var result = new PluginContextResult(context, null, true);
        return result;
    }
}
