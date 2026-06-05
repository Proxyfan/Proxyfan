using Proxyfan.Domain.Configuration.Migration;
using System;
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
    ///     Gets the <see cref="Exception" /> raised by a file-system operation during load
    ///     (read, backup creation, or write), or <see langword="null" /> when no
    ///     file-system error occurred. A non-<see langword="null" /> value means the
    ///     configuration could not be fully loaded or persisted; callers should log the
    ///     failure and treat the <see cref="Snapshot" /> as a best-effort or default
    ///     result.
    /// </summary>
    public required Exception? IoFailure { get; init; }

    /// <summary>
    ///     Gets the raw trimmed text of any lines in the configuration file that could not
    ///     be parsed as <c>key=value</c> pairs, or <see langword="null" /> when the file
    ///     was not read (e.g. it was missing). An empty list indicates a clean parse with
    ///     no malformed lines. A non-empty list indicates the file was rejected: no
    ///     migration was applied and the file was not rewritten.
    /// </summary>
    public required IReadOnlyList<string>? MalformedLines { get; init; }

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
