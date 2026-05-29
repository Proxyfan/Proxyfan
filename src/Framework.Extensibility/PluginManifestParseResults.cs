namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Factory helpers for constructing <see cref="PluginManifestParseResult" /> instances.
/// </summary>
public static class PluginManifestParseResults
{
    /// <summary>
    ///     Constructs a failure result with the supplied error message.
    /// </summary>
    /// <param name="errorMessage">The failure description.</param>
    /// <returns>The failure result.</returns>
    public static PluginManifestParseResult Failure(string errorMessage)
    {
        var result = new PluginManifestParseResult(null, errorMessage, false);
        return result;
    }

    /// <summary>
    ///     Constructs a success result with the supplied manifest.
    /// </summary>
    /// <param name="manifest">The parsed manifest.</param>
    /// <returns>The success result.</returns>
    public static PluginManifestParseResult Success(PluginManifest manifest)
    {
        var result = new PluginManifestParseResult(manifest, null, true);
        return result;
    }
}
