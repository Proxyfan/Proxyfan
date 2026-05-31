using System;
using System.Globalization;

namespace Proxyfan.Cli;

/// <summary>
///     Parses raw CLI arguments into a <see cref="CliCommand" />.
/// </summary>
public static class CliArgumentParser
{
    private const int DefaultPort = 8080;

    /// <summary>
    ///     Parses the supplied arguments into a typed <see cref="CliCommand" />. Returns
    ///     <see cref="CliCommandKind.Help" /> when no arguments are present and
    ///     <see cref="CliCommandKind.Unknown" /> for an unrecognized command.
    /// </summary>
    /// <param name="args">The raw arguments.</param>
    /// <returns>The parsed command.</returns>
    public static CliCommand Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new CliCommand(CliCommandKind.Help, DefaultPort, null);
        }

        var command = args[0];

        if (HasHelpToken(command))
        {
            return new CliCommand(CliCommandKind.Help, DefaultPort, null);
        }

        if (HasVersionToken(command))
        {
            return new CliCommand(CliCommandKind.Version, DefaultPort, null);
        }

        var typedCommand = TryParseTypedCommand(command, args);
        if (typedCommand is not null)
        {
            return typedCommand;
        }

        return new CliCommand(CliCommandKind.Unknown, DefaultPort, null);
    }

    private static string? ExtractPath(string[] args)
    {
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], "--input", StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        if (args.Length >= 2)
        {
            return args[1];
        }

        return null;
    }

    private static int ExtractPort(string[] args)
    {
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], "--port", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
                && port is >= 1 and <= 65535)
            {
                return port;
            }
        }

        return DefaultPort;
    }

    private static int? ExtractPositiveInt(string[] args, string optionName)
    {
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                && parsed > 0)
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? ExtractStringOption(string[] args, string optionName)
    {
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static bool HasHelpToken(string token)
    {
        return string.Equals(token, "--help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "-h", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "help", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasVersionToken(string token)
    {
        return string.Equals(token, "--version", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "-v", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "version", StringComparison.OrdinalIgnoreCase);
    }

    private static CliCommand? TryParseTypedCommand(string command, string[] args)
    {
        if (string.Equals(command, "start", StringComparison.OrdinalIgnoreCase))
        {
            var port = ExtractPort(args);
            var startOptions = new CliStartOptions
            {
                OutputPath = ExtractStringOption(args, "--output"),
                DurationSeconds = ExtractPositiveInt(args, "--duration"),
            };
            return new CliCommand(CliCommandKind.Start, port, null)
            {
                StartOptions = startOptions,
            };
        }

        if (string.Equals(command, "har-summary", StringComparison.OrdinalIgnoreCase))
        {
            var path = ExtractPath(args);
            return new CliCommand(CliCommandKind.HarSummary, DefaultPort, path);
        }

        if (string.Equals(command, "har-to-curl", StringComparison.OrdinalIgnoreCase))
        {
            var path = ExtractPath(args);
            return new CliCommand(CliCommandKind.HarToCurl, DefaultPort, path);
        }

        if (string.Equals(command, "har-filter", StringComparison.OrdinalIgnoreCase))
        {
            var options = CliHarFilterArgumentParser.Parse(args);
            return new CliCommand(CliCommandKind.HarFilter, DefaultPort, options?.InputPath)
            {
                HarFilterOptions = options,
            };
        }

        if (string.Equals(command, "har-stats", StringComparison.OrdinalIgnoreCase))
        {
            var path = ExtractPath(args);
            return new CliCommand(CliCommandKind.HarStats, DefaultPort, path);
        }

        if (string.Equals(command, "send", StringComparison.OrdinalIgnoreCase))
        {
            var sendRequest = CliSendArgumentParser.Parse(args);
            return new CliCommand(CliCommandKind.Send, DefaultPort, null, sendRequest);
        }

        return null;
    }
}
