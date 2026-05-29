using System;

namespace Proxyfan.Cli;

/// <summary>
///     Parses the arguments specific to the <c>har-filter</c> command into a
///     <see cref="CliHarFilterOptions" />. Recognises <c>--input</c>, <c>--output</c>, and
///     <c>--pattern</c> flags. Returns <see langword="null" /> when any required argument is
///     missing so that <see cref="CliHarFilterHandler" /> can surface a helpful error message.
/// </summary>
public static class CliHarFilterArgumentParser
{
    /// <summary>
    ///     Parses the supplied arguments into a <see cref="CliHarFilterOptions" />.
    /// </summary>
    /// <param name="args">The raw CLI arguments (including the verb).</param>
    /// <returns>The parsed options, or <see langword="null" /> when any required flag is missing.</returns>
    public static CliHarFilterOptions? Parse(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;
        string? pattern = null;

        for (var index = 1; index < args.Length - 1; index++)
        {
            var flag = args[index];

            if (string.Equals(flag, "--input", StringComparison.OrdinalIgnoreCase))
            {
                inputPath = args[index + 1];
            }
            else if (string.Equals(flag, "--output", StringComparison.OrdinalIgnoreCase))
            {
                outputPath = args[index + 1];
            }
            else if (string.Equals(flag, "--pattern", StringComparison.OrdinalIgnoreCase))
            {
                pattern = args[index + 1];
            }
        }

        if (string.IsNullOrWhiteSpace(inputPath)
            || string.IsNullOrWhiteSpace(outputPath)
            || string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        var options = new CliHarFilterOptions
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            Pattern = pattern,
        };
        return options;
    }
}
