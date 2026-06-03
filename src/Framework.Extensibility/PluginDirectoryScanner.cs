using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Scans the on-disk plugins root (e.g. <c>%LOCALAPPDATA%\Proxyfan\plugins\</c>) for
///     immediate sub-directories containing a <c>plugin.manifest</c> file. Each sub-directory
///     produces a <see cref="PluginCandidate" /> recording either the parsed manifest or a
///     load-time error. Missing or unreadable root directories yield an empty result; the
///     scanner never throws on bad input.
/// </summary>
public sealed class PluginDirectoryScanner
{
    /// <summary>
    ///     The well-known file name read from each plugin directory.
    /// </summary>
    public const string ManifestFileName = "plugin.manifest";

    /// <summary>
    ///     Scans <paramref name="rootDirectory" /> and returns one candidate per sub-directory.
    /// </summary>
    /// <param name="rootDirectory">The plugins root directory.</param>
    /// <returns>The candidate list (empty when the root does not exist).</returns>
    public IReadOnlyList<PluginCandidate> Scan(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        var candidates = new List<PluginCandidate>();

        if (!Directory.Exists(rootDirectory))
        {
            return candidates;
        }

        string[] subdirectories;
        try
        {
            subdirectories = Directory.GetDirectories(rootDirectory);
        }
        catch (DirectoryNotFoundException)
        {
            return candidates;
        }
        catch (UnauthorizedAccessException)
        {
            return candidates;
        }
        catch (IOException)
        {
            return candidates;
        }

        Array.Sort(subdirectories, StringComparer.OrdinalIgnoreCase);
        foreach (var directory in subdirectories)
        {
            var candidate = PluginCandidateBuilder.Build(directory, ManifestFileName);
            candidates.Add(candidate);
        }

        return candidates;
    }
}
