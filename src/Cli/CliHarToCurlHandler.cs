using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Handles execution of the <see cref="CliCommandKind.HarToCurl" /> command, which reads
///     a HAR file and prints one cURL command per captured flow.
/// </summary>
public static class CliHarToCurlHandler
{
    /// <summary>
    ///     Runs the har-to-curl command and returns an exit code (0 for success).
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
            await standardError.WriteAsync("har-to-curl requires a file path argument.".AsMemory(), cancellationToken).ConfigureAwait(false);
            return 7;
        }

        if (!File.Exists(command.PathArgument))
        {
            await standardError.WriteAsync(("File not found: " + command.PathArgument).AsMemory(), cancellationToken).ConfigureAwait(false);
            return 8;
        }

        var importer = new HarImporter();
        await using var stream = File.OpenRead(command.PathArgument);
        var flows = await importer.ImportAsync(stream, cancellationToken).ConfigureAwait(false);

        for (var index = 0; index < flows.Count; index++)
        {
            var request = flows[index].Request;
            if (request is null)
            {
                continue;
            }

            var curl = CurlCommandConverter.ToCurl(request);
            await standardOut.WriteLineAsync(curl.AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }
}
