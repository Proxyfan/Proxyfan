using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Coordinates execution of a parsed <see cref="CliCommand" /> and writes results to the
///     supplied text writers. Designed for testability — no console dependencies.
/// </summary>
public sealed class CliRunner
{
    private const string ProductVersion = "1.0.0";

    /// <summary>
    ///     Executes the supplied command and writes output to the appropriate writer.
    /// </summary>
    /// <param name="command">The parsed command to execute.</param>
    /// <param name="standardOut">Standard output writer.</param>
    /// <param name="standardError">Standard error writer.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The process exit code (0 for success).</returns>
    public async Task<int> RunAsync(
        CliCommand command,
        TextWriter standardOut,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        switch (command.Kind)
        {
            case CliCommandKind.Help:
                await CliHelpWriter.WriteHelpAsync(standardOut, cancellationToken).ConfigureAwait(false);
                return 0;

            case CliCommandKind.Version:
                await standardOut.WriteAsync(("Proxyfan CLI " + ProductVersion).AsMemory(), cancellationToken).ConfigureAwait(false);
                return 0;

            case CliCommandKind.Start:
                await standardOut.WriteAsync(("Proxy server would start on port " + command.Port.ToString(CultureInfo.InvariantCulture)).AsMemory(), cancellationToken).ConfigureAwait(false);
                return 0;

            case CliCommandKind.HarSummary:
                return await CliHarSummaryHandler.RunAsync(command, standardOut, standardError, cancellationToken).ConfigureAwait(false);

            case CliCommandKind.HarToCurl:
                return await CliHarToCurlHandler.RunAsync(command, standardOut, standardError, cancellationToken).ConfigureAwait(false);

            case CliCommandKind.HarFilter:
                return await CliHarFilterHandler.RunAsync(command, standardOut, standardError, cancellationToken).ConfigureAwait(false);

            case CliCommandKind.HarStats:
                return await CliHarStatsHandler.RunAsync(command, standardOut, standardError, cancellationToken).ConfigureAwait(false);

            case CliCommandKind.Send:
                return await CliSendHandler.RunAsync(command, standardOut, standardError, cancellationToken).ConfigureAwait(false);

            case CliCommandKind.Unknown:
                await standardError.WriteAsync("Unknown command. Run 'help' for usage.".AsMemory(), cancellationToken).ConfigureAwait(false);
                return 2;

            default:
                await standardError.WriteAsync("Unhandled command kind.".AsMemory(), cancellationToken).ConfigureAwait(false);
                return 3;
        }
    }
}
