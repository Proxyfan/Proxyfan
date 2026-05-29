using System;
using System.IO;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Builds <see cref="PluginCandidate" /> entries from a single on-disk plugin
///     directory by locating the manifest file, reading it, and delegating to
///     <see cref="PluginManifestReader.Parse" />. All filesystem and parse failures are
///     captured as invalid candidates so that one bad plugin never disrupts the scan.
/// </summary>
public static class PluginCandidateBuilder
{
    /// <summary>
    ///     Builds a candidate for the supplied directory.
    /// </summary>
    /// <param name="directoryPath">The plugin directory path.</param>
    /// <param name="manifestFileName">The well-known manifest file name to look for.</param>
    /// <returns>The candidate (valid or invalid).</returns>
    public static PluginCandidate Build(string directoryPath, string manifestFileName)
    {
        var manifestPath = Path.Combine(directoryPath, manifestFileName);
        if (!File.Exists(manifestPath))
        {
            return PluginCandidates.Invalid(directoryPath, $"Missing manifest '{manifestFileName}'.");
        }

        string text;
        try
        {
            text = File.ReadAllText(manifestPath);
        }
        catch (IOException ex)
        {
            return PluginCandidates.Invalid(directoryPath, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return PluginCandidates.Invalid(directoryPath, ex.Message);
        }

        var result = PluginManifestReader.Parse(text);
        if (!result.IsSuccess || result.Manifest is null)
        {
            return PluginCandidates.Invalid(directoryPath, result.ErrorMessage ?? "Manifest parse failed.");
        }

        return PluginCandidates.Valid(directoryPath, result.Manifest);
    }
}
