namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Factory helpers for constructing <see cref="PluginCandidate" /> instances.
/// </summary>
public static class PluginCandidates
{
    /// <summary>
    ///     Constructs an invalid candidate (no manifest, parse error, etc.).
    /// </summary>
    /// <param name="directoryPath">The absolute path of the candidate directory.</param>
    /// <param name="errorMessage">A human-readable description of the problem.</param>
    /// <returns>The invalid candidate.</returns>
    public static PluginCandidate Invalid(string directoryPath, string errorMessage)
    {
        var candidate = new PluginCandidate(directoryPath, null, errorMessage, false);
        return candidate;
    }

    /// <summary>
    ///     Constructs a valid candidate with the parsed manifest.
    /// </summary>
    /// <param name="directoryPath">The absolute path of the candidate directory.</param>
    /// <param name="manifest">The parsed manifest.</param>
    /// <returns>The valid candidate.</returns>
    public static PluginCandidate Valid(string directoryPath, PluginManifest manifest)
    {
        var candidate = new PluginCandidate(directoryPath, manifest, null, true);
        return candidate;
    }
}
