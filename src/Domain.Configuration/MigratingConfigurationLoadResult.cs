using Proxyfan.Domain.Configuration.Migration;
using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Result of an <see cref="IMigratingConfigurationLoader.Load" /> call. Bundles the
///     migrated snapshot with the pipeline result so callers can log the actions that were
///     applied (or detect that no migration was necessary). When the configuration file
///     contained malformed lines, <see cref="MalformedLineNumbers" /> is non-empty and
///     neither migration nor file rewriting was performed.
/// </summary>
public sealed class MigratingConfigurationLoadResult
{
    /// <summary>
    ///     Gets the path to the backup of the original file written when a migration was
    ///     applied, or <see langword="null" /> when no backup was created (because the file
    ///     was missing, no migration occurred, or the file contained malformed lines).
    /// </summary>
    public required string? BackupPath { get; init; }

    /// <summary>
    ///     Gets the 1-based line numbers of any lines in the configuration file that were
    ///     malformed (neither empty, nor a comment, nor a valid <c>key=value</c> pair).
    ///     When non-empty, the loader did not apply migration and did not rewrite the file.
    /// </summary>
    public required IReadOnlyList<int> MalformedLineNumbers { get; init; }

    /// <summary>
    ///     Gets the migration pipeline result containing the version transition record and
    ///     the list of actions that were applied.
    /// </summary>
    public required ConfigurationMigrationPipelineResult PipelineResult { get; init; }

    /// <summary>
    ///     Gets the (possibly migrated) configuration snapshot ready for consumption.
    /// </summary>
    public required ConfigurationSnapshot Snapshot { get; init; }
}
