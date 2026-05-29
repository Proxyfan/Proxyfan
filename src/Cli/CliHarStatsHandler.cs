using Proxyfan.Domain.Session.Har;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Handles execution of the <see cref="CliCommandKind.HarStats" /> command, which
///     prints aggregated statistics (status-class distribution, method distribution, body
///     byte totals, request duration min/median/max) for a captured HAR file.
/// </summary>
public static class CliHarStatsHandler
{
    /// <summary>
    ///     Runs the har-stats command and returns a process exit code.
    /// </summary>
    /// <param name="command">The parsed command (must have <c>PathArgument</c> set).</param>
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
        if (string.IsNullOrWhiteSpace(command.PathArgument))
        {
            await standardError.WriteAsync("har-stats requires a file path argument.".AsMemory(), cancellationToken).ConfigureAwait(false);
            return 11;
        }

        if (!File.Exists(command.PathArgument))
        {
            await standardError.WriteAsync(("File not found: " + command.PathArgument).AsMemory(), cancellationToken).ConfigureAwait(false);
            return 12;
        }

        var importer = new HarImporter();
        await using var stream = File.OpenRead(command.PathArgument);
        var flows = await importer.ImportAsync(stream, cancellationToken).ConfigureAwait(false);

        var report = HarStatsFormatter.BuildReport(flows);
        await standardOut.WriteAsync(report.AsMemory(), cancellationToken).ConfigureAwait(false);
        return 0;
    }
}
