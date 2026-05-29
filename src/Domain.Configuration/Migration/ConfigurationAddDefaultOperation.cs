using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     Populates a newly introduced configuration key with its default value. If the key
///     already exists in the working set the operation is a no-op so that user-supplied
///     values are never overwritten by defaults.
/// </summary>
public sealed class ConfigurationAddDefaultOperation : IConfigurationMigrationOperation
{
    /// <summary>
    ///     Gets the default value to insert when the key is absent.
    /// </summary>
    public required string DefaultValue { get; init; }

    /// <summary>
    ///     Gets the configuration key being introduced.
    /// </summary>
    public required string Key { get; init; }

    /// <inheritdoc />
    public void Apply(Dictionary<string, string> values, List<ConfigurationMigrationAction> actions)
    {
        if (values.ContainsKey(Key))
        {
            return;
        }

        values[Key] = DefaultValue;
        var action = new ConfigurationMigrationAction
        {
            Key = Key,
            Kind = ConfigurationMigrationActionKind.DefaultAdded,
            Value = DefaultValue,
        };
        actions.Add(action);
    }
}
