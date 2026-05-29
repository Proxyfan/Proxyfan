using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     A single primitive transformation applied to a mutable working dictionary during a
///     configuration migration step. Composed together inside a
///     <see cref="ConfigurationMigrator" /> to express a complete schema-version transition.
/// </summary>
public interface IConfigurationMigrationOperation
{
    /// <summary>
    ///     Applies the operation to <paramref name="values" />, mutating the dictionary in
    ///     place and recording any actions performed in <paramref name="actions" />.
    /// </summary>
    /// <param name="values">The mutable working set of configuration values.</param>
    /// <param name="actions">The list to which performed actions are appended.</param>
    void Apply(Dictionary<string, string> values, List<ConfigurationMigrationAction> actions);
}
