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
        var validationExitCode = await ValidateCommandAsync(command, standardError, cancellationToken).ConfigureAwait(false);
        if (validationExitCode.HasValue)
        {
            return validationExitCode.Value;
        }

        var importer = new HarImporter();
        await using var stream = File.OpenRead(command.PathArgument!);
        var flows = await importer.ImportAsync(stream, cancellationToken).ConfigureAwait(false);

        var report = command.IsJsonOutput
            ? HarStatsFormatter.BuildJsonReport(flows)
            : HarStatsFormatter.BuildReport(flows);
        await standardOut.WriteAsync(report.AsMemory(), cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static async Task<int?> ValidateCommandAsync(CliCommand command, TextWriter standardError, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.PathArgument))
        {
            var error = new HarStatsHandlerError
            {
                ExitCode = 11,
                Message = "har-stats requires a file path argument.",
            };
            await WriteErrorAsync(command, standardError, error, cancellationToken).ConfigureAwait(false);
            return 11;
        }

        if (!File.Exists(command.PathArgument))
        {
            var error = new HarStatsHandlerError
            {
                ExitCode = 12,
                Message = "File not found: " + command.PathArgument,
            };
            await WriteErrorAsync(command, standardError, error, cancellationToken).ConfigureAwait(false);
            return 12;
        }

        return null;
    }

    private static Task WriteErrorAsync(
        CliCommand command,
        TextWriter standardError,
        HarStatsHandlerError error,
        CancellationToken cancellationToken)
    {
        if (!command.IsJsonOutput)
        {
            return standardError.WriteAsync(error.Message.AsMemory(), cancellationToken);
        }

        var payload = new
        {
            exitCode = error.ExitCode,
            status = "error",
            error = error.Message,
        };
        return CliJsonWriter.WriteLineAsync(standardError, payload, cancellationToken);
    }

    /// <summary>
    ///     Small parameter object for a validation failure reported by the har-stats handler.
    /// </summary>
    private sealed class HarStatsHandlerError
    {
        public required int ExitCode { get; init; }

        /// <summary>
        ///     Gets the error message to write to stderr (or the JSON error payload).
        /// </summary>
        public required string Message { get; init; }
    }
}
