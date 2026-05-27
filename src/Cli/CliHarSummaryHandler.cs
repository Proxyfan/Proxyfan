using Proxyfan.Domain.Session.Har;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Handles execution of the <see cref="CliCommandKind.HarSummary" /> command.
/// </summary>
public static class CliHarSummaryHandler
{
    /// <summary>
    ///     Runs the HAR summary command.
    /// </summary>
    /// <param name="command">The parsed command containing the path argument.</param>
    /// <param name="standardOut">Standard output writer.</param>
    /// <param name="standardError">Standard error writer.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The process exit code (0 for success).</returns>
    public static async Task<int> RunAsync(
        CliCommand command,
        TextWriter standardOut,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.PathArgument))
        {
            await standardError.WriteAsync("har-summary requires a file path argument.".AsMemory(), cancellationToken).ConfigureAwait(false);
            return 4;
        }

        if (!File.Exists(command.PathArgument))
        {
            await standardError.WriteAsync(("File not found: " + command.PathArgument).AsMemory(), cancellationToken).ConfigureAwait(false);
            return 5;
        }

        var importer = new HarImporter();
        var renderer = new HarSummaryRenderer(importer);
        await using var stream = File.OpenRead(command.PathArgument);
        await renderer.RenderAsync(stream, standardOut, cancellationToken).ConfigureAwait(false);
        return 0;
    }
}
