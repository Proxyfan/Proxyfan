namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     Identifies the kind of transformation applied by a single configuration migration step.
/// </summary>
public enum ConfigurationMigrationActionKind
{
    /// <summary>
    ///     A configuration key was renamed to a new name. Its value is preserved.
    /// </summary>
    Renamed,

    /// <summary>
    ///     A configuration key was removed from the active schema and preserved under
    ///     <c>_deprecated.&lt;original-key&gt;</c> so a future rollback can recover it.
    /// </summary>
    Deprecated,

    /// <summary>
    ///     A new configuration key was introduced and populated with its default value.
    /// </summary>
    DefaultAdded,

    /// <summary>
    ///     The schema version key was bumped to the migrator's target version.
    /// </summary>
    VersionBumped,
}
