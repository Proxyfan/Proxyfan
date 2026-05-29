using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     The result of applying a single <see cref="IConfigurationMigrator" /> to a
///     configuration snapshot: the transformed values plus a record of every action applied.
/// </summary>
public sealed class ConfigurationMigratorResult
{
    /// <summary>
    ///     Gets the ordered list of actions applied by this migrator.
    /// </summary>
    public required IReadOnlyList<ConfigurationMigrationAction> Actions { get; init; }

    /// <summary>
    ///     Gets the transformed configuration values.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Values { get; init; }
}
