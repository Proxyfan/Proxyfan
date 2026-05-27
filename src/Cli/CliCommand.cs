namespace Proxyfan.Cli;

/// <summary>
///     Parsed CLI command and arguments produced by <see cref="CliArgumentParser" />.
/// </summary>
public sealed class CliCommand
{
    /// <summary>
    ///     Gets the parsed command kind.
    /// </summary>
    public CliCommandKind Kind { get; }

    /// <summary>
    ///     Gets the optional path argument (e.g. HAR file path for HarSummary).
    /// </summary>
    public string? PathArgument { get; }

    /// <summary>
    ///     Gets the optional port argument (for the Start command). Defaults to 8080 when absent.
    /// </summary>
    public int Port { get; }

    /// <summary>
    ///     Gets the parsed Send-command arguments (method, URL, headers, body) when this is a
    ///     <see cref="CliCommandKind.Send" /> command, otherwise <see langword="null" />.
    /// </summary>
    public CliSendRequest? SendRequest { get; }

    /// <summary>
    ///     Initializes a new <see cref="CliCommand" />.
    /// </summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="port">The optional port.</param>
    /// <param name="pathArgument">The optional path argument.</param>
    public CliCommand(CliCommandKind kind, int port, string? pathArgument)
        : this(kind, port, pathArgument, null)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="CliCommand" /> with all four arguments.
    /// </summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="port">The optional port.</param>
    /// <param name="pathArgument">The optional path argument.</param>
    /// <param name="sendRequest">The optional send-command arguments.</param>
    public CliCommand(CliCommandKind kind, int port, string? pathArgument, CliSendRequest? sendRequest)
    {
        Kind = kind;
        Port = port;
        PathArgument = pathArgument;
        SendRequest = sendRequest;
    }
}
