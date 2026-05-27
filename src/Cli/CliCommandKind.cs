namespace Proxyfan.Cli;

/// <summary>
///     Identifies the parsed CLI command kind.
/// </summary>
public enum CliCommandKind
{
    /// <summary>
    ///     The arguments did not match any known command.
    /// </summary>
    Unknown,

    /// <summary>
    ///     The user requested help / usage information.
    /// </summary>
    Help,

    /// <summary>
    ///     The user requested version information.
    /// </summary>
    Version,

    /// <summary>
    ///     The user requested to start the proxy server.
    /// </summary>
    Start,

    /// <summary>
    ///     The user requested to convert a HAR file into a readable summary.
    /// </summary>
    HarSummary,
}
