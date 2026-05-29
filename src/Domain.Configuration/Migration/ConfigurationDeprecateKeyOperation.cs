using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     Removes a configuration key from the active schema while preserving its value under
///     <c>_deprecated.&lt;Key&gt;</c> so a rollback to a previous version can restore it.
///     If the key is not present in the working set the operation is a no-op.
/// </summary>
public sealed class ConfigurationDeprecateKeyOperation : IConfigurationMigrationOperation
{
    /// <summary>
    ///     Gets the configuration key being deprecated.
    /// </summary>
    public required string Key { get; init; }

    /// <inheritdoc />
    public void Apply(Dictionary<string, string> values, List<ConfigurationMigrationAction> actions)
    {
        if (!values.TryGetValue(Key, out var existingValue))
        {
            return;
        }

        values.Remove(Key);
        var deprecatedKey = ConfigurationMigrationConstants.DeprecatedKeyPrefix + Key;
        values[deprecatedKey] = existingValue;
        var action = new ConfigurationMigrationAction
        {
            Key = Key,
            Kind = ConfigurationMigrationActionKind.Deprecated,
            SecondaryKey = deprecatedKey,
            Value = existingValue,
        };
        actions.Add(action);
    }
}
