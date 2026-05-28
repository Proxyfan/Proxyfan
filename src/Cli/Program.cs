using System;
using System.Threading;

namespace Proxyfan.Cli;

/// <summary>
///     Program entry point for the Proxyfan CLI.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "CLI entry point: process-level wiring (Console.CancelKeyPress, blocking GetResult) not unit-testable.")]
public static class Program
{
    /// <summary>
    ///     CLI entry point. Parses arguments and delegates to <see cref="CliRunner" />.
    /// </summary>
    /// <param name="args">The raw command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static int Main(string[] args)
    {
        var command = CliArgumentParser.Parse(args);
        var runner = new CliRunner();
        using var cancellationSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        return runner.RunAsync(command, Console.Out, Console.Error, cancellationSource.Token)
                     .GetAwaiter()
                     .GetResult();
    }
}
