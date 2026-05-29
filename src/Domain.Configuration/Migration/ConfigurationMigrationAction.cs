namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     Describes a single configuration migration action that was applied to a configuration
///     snapshot, used for diagnostic logging and audit trails.
/// </summary>
public sealed record ConfigurationMigrationAction
{
    /// <summary>
    ///     Gets the configuration key the action targeted. For <see cref="ConfigurationMigrationActionKind.VersionBumped" />
    ///     this is <see cref="ConfigurationMigrationConstants.VersionKey" />.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    ///     Gets the kind of action that was applied.
    /// </summary>
    public required ConfigurationMigrationActionKind Kind { get; init; }

    /// <summary>
    ///     Gets the optional secondary key for the action. For
    ///     <see cref="ConfigurationMigrationActionKind.Renamed" /> this is the new key; for
    ///     <see cref="ConfigurationMigrationActionKind.Deprecated" /> this is the deprecated
    ///     key (i.e. <c>_deprecated.&lt;Key&gt;</c>); otherwise <see langword="null" />.
    /// </summary>
    public string? SecondaryKey { get; init; }

    /// <summary>
    ///     Gets the optional value associated with the action. For
    ///     <see cref="ConfigurationMigrationActionKind.DefaultAdded" /> this is the inserted
    ///     default value; for <see cref="ConfigurationMigrationActionKind.VersionBumped" />
    ///     this is the new version string; otherwise <see langword="null" />.
    /// </summary>
    public string? Value { get; init; }
}
