using Proxyfan.Domain.Configuration.Migration;
using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Result of an <see cref="IMigratingConfigurationLoader.Load" /> call. Bundles the
///     migrated snapshot with the pipeline result so callers can log the actions that were
///     applied (or detect that no migration was necessary).
/// </summary>
public sealed class MigratingConfigurationLoadResult
{
    /// <summary>
    ///     Gets the path to the backup of the original file written when a migration was
    ///     applied, or <see langword="null" /> when no backup was created (because the file
    ///     was missing or no migration occurred).
    /// </summary>
    public required string? BackupPath { get; init; }

    /// <summary>
    ///     Gets diagnostics for malformed non-empty, non-comment lines encountered during
    ///     parse. An empty collection indicates parsing succeeded without malformed lines.
    /// </summary>
    public required IReadOnlyList<KeyValueConfigurationParseDiagnostic> ParseDiagnostics { get; init; }

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
