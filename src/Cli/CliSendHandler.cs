using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Handles execution of the <see cref="CliCommandKind.Send" /> command. Prints the
///     composed request as a wire-format HTTP/1.1 message to the supplied output writer.
///     (Network execution is left to the caller / future enhancement.)
/// </summary>
public static class CliSendHandler
{
    /// <summary>
    ///     Runs the send command and returns an exit code (0 for success).
    /// </summary>
    /// <param name="command">The parsed command (must have a non-null <c>SendRequest</c>).</param>
    /// <param name="standardOut">Standard output writer.</param>
    /// <param name="standardError">Standard error writer.</param>
    /// <param name="cancellationToken">A token that cancels writes.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(
        CliCommand command,
        TextWriter standardOut,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        if (command.SendRequest is null)
        {
            await standardError.WriteAsync("send requires --url and optionally --method/--header/--body.".AsMemory(), cancellationToken).ConfigureAwait(false);
            return 6;
        }

        var formatted = CliSendFormatter.Format(command.SendRequest);
        await standardOut.WriteAsync(formatted.AsMemory(), cancellationToken).ConfigureAwait(false);
        return 0;
    }
}
