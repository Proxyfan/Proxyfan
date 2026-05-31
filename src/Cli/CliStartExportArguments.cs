using Microsoft.Extensions.Hosting;
using System.IO;

namespace Proxyfan.Cli;

/// <summary>
///     Parameter object for <see cref="CliStartHandler" />'s internal export helper. Used to
///     keep the helper below the 4-parameter limit enforced by ATXCS022.
/// </summary>
public sealed class CliStartExportArguments
{
    /// <summary>
    ///     Gets the host used to resolve the traffic store and HAR exporter.
    /// </summary>
    public required IHost Host { get; init; }

    /// <summary>
    ///     Gets the standard-error writer used to report export failures.
    /// </summary>
    public required TextWriter StandardError { get; init; }

    /// <summary>
    ///     Gets the standard-output writer used to report export success.
    /// </summary>
    public required TextWriter StandardOut { get; init; }

    /// <summary>
    ///     Gets the parsed start options (used to read the output path).
    /// </summary>
    public required CliStartOptions StartOptions { get; init; }
}
