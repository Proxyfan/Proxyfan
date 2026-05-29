using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     The result of running a multi-step configuration migration pipeline: the original
///     source version, the resulting target version, the transformed values, and the full
///     audit trail of every action applied across every migrator that ran.
/// </summary>
public sealed class ConfigurationMigrationPipelineResult
{
    /// <summary>
    ///     Gets the ordered list of actions applied across every chained migrator.
    /// </summary>
    public required IReadOnlyList<ConfigurationMigrationAction> Actions { get; init; }

    /// <summary>
    ///     Gets a value indicating whether any migrator actually ran. <see langword="false" />
    ///     when the source version already matched the target version, in which case
    ///     <see cref="Values" /> is the unchanged input snapshot.
    /// </summary>
    public required bool IsMigrated { get; init; }

    /// <summary>
    ///     Gets the schema version the pipeline started from.
    /// </summary>
    public required ConfigurationVersion SourceVersion { get; init; }

    /// <summary>
    ///     Gets the schema version the pipeline finished at.
    /// </summary>
    public required ConfigurationVersion TargetVersion { get; init; }

    /// <summary>
    ///     Gets the transformed configuration values.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Values { get; init; }
}
