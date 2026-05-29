namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     Well-known configuration migration constants — the key under which the configuration
///     schema version is recorded and the prefix used to preserve deprecated/unknown keys
///     so they survive future rollbacks.
/// </summary>
public static class ConfigurationMigrationConstants
{
    /// <summary>
    ///     The prefix used to preserve removed/unknown keys when migrating to a newer schema.
    ///     Keys carrying this prefix are not interpreted by the application but are retained
    ///     so they can be restored by a rollback to a previous version.
    /// </summary>
    public const string DeprecatedKeyPrefix = "_deprecated.";

    /// <summary>
    ///     The key under which the configuration schema version is persisted in the
    ///     configuration file.
    /// </summary>
    public const string VersionKey = "version";
}
