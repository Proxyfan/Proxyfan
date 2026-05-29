namespace Proxyfan.Cli;

/// <summary>
///     Options for the <see cref="CliCommandKind.HarFilter" /> command. Bundles the input
///     HAR file path, the output HAR file path, and the URL pattern used to select flows.
/// </summary>
public sealed class CliHarFilterOptions
{
    /// <summary>
    ///     Gets the path of the input HAR file.
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    ///     Gets the path of the output HAR file.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    ///     Gets the URL pattern used to select flows. Supports the same wildcard syntax as
    ///     the rule engine (e.g. <c>*.example.com/api/*</c>).
    /// </summary>
    public required string Pattern { get; init; }
}
