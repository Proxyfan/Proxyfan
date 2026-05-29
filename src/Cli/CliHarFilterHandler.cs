using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Handles execution of the <see cref="CliCommandKind.HarFilter" /> command, which
///     reads a HAR file, keeps only flows whose request URL matches a wildcard pattern, and
///     writes the result to a new HAR file. Useful in CI/CD pipelines for slicing a large
///     capture down to the calls of interest.
/// </summary>
public static class CliHarFilterHandler
{
    /// <summary>
    ///     Runs the har-filter command and returns a process exit code.
    /// </summary>
    /// <param name="command">The parsed command (must have <see cref="CliCommand.HarFilterOptions" /> set).</param>
    /// <param name="standardOut">Standard output writer.</param>
    /// <param name="standardError">Standard error writer.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(
        CliCommand command,
        TextWriter standardOut,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        var options = command.HarFilterOptions;

        if (options is null)
        {
            await standardError.WriteAsync(
                "har-filter requires --input, --output, and --pattern arguments.".AsMemory(),
                cancellationToken).ConfigureAwait(false);
            return 9;
        }

        if (!File.Exists(options.InputPath))
        {
            await standardError.WriteAsync(("File not found: " + options.InputPath).AsMemory(), cancellationToken).ConfigureAwait(false);
            return 10;
        }

        var importer = new HarImporter();
        IReadOnlyList<TrafficFlow> imported;

        await using (var input = File.OpenRead(options.InputPath))
        {
            imported = await importer.ImportAsync(input, cancellationToken).ConfigureAwait(false);
        }

        var matcher = new WildcardUrlMatcher(options.Pattern);
        var matched = new List<TrafficFlow>(imported.Count);

        foreach (var flow in imported)
        {
            if (HasMatchingFlow(flow, matcher))
            {
                matched.Add(flow);
            }
        }

        var outputDirectory = Path.GetDirectoryName(options.OutputPath);

        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var exporter = new HarExporter();

        await using (var output = File.Create(options.OutputPath))
        {
            await exporter.ExportAsync(matched, output, cancellationToken).ConfigureAwait(false);
        }

        await standardOut.WriteAsync(
            ("Filtered " + matched.Count + " flow(s) of " + imported.Count + " into " + options.OutputPath).AsMemory(),
            cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static bool HasMatchingFlow(TrafficFlow flow, WildcardUrlMatcher matcher)
    {
        if (flow.Request is null)
        {
            return false;
        }

        return matcher.HasMatch(flow.Request.RequestUri.ToString());
    }
}
