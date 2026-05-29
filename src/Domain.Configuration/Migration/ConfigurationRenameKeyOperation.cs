using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration.Migration;

/// <summary>
///     Renames a configuration key, preserving its value. If <see cref="OldKey" /> is not
///     present in the working set the operation is a no-op. If <see cref="NewKey" /> already
///     exists the existing value wins and the old key is dropped.
/// </summary>
public sealed class ConfigurationRenameKeyOperation : IConfigurationMigrationOperation
{
    /// <summary>
    ///     Gets the configuration key the value is moved to.
    /// </summary>
    public required string NewKey { get; init; }

    /// <summary>
    ///     Gets the configuration key being renamed away.
    /// </summary>
    public required string OldKey { get; init; }

    /// <inheritdoc />
    public void Apply(Dictionary<string, string> values, List<ConfigurationMigrationAction> actions)
    {
        if (!values.TryGetValue(OldKey, out var existingValue))
        {
            return;
        }

        values.Remove(OldKey);
        if (!values.ContainsKey(NewKey))
        {
            values[NewKey] = existingValue;
        }

        var action = new ConfigurationMigrationAction
        {
            Key = OldKey,
            Kind = ConfigurationMigrationActionKind.Renamed,
            SecondaryKey = NewKey,
            Value = existingValue,
        };
        actions.Add(action);
    }
}
