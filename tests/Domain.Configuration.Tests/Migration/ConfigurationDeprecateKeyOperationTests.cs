using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Configuration.Migration;

namespace Proxyfan.Domain.Configuration.Tests.Migration;

/// <summary>
///     Tests for <see cref="ConfigurationDeprecateKeyOperation" />.
/// </summary>
public sealed class ConfigurationDeprecateKeyOperationTests
{
    /// <summary>
    ///     When the key is absent the operation is a no-op.
    /// </summary>
    [Test]
    public async Task Apply_KeyAbsent_DoesNothing()
    {
        var values = new Dictionary<string, string>();
        var actions = new List<ConfigurationMigrationAction>();
        var operation = new ConfigurationDeprecateKeyOperation
        {
            Key = "experimental.feature",
        };

        operation.Apply(values, actions);

        await Assert.That(values.Count).IsEqualTo(0);
        await Assert.That(actions.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     When the key is present the value moves under the _deprecated prefix and the action
    ///     is recorded.
    /// </summary>
    [Test]
    public async Task Apply_KeyPresent_MovesValueToDeprecatedNamespace()
    {
        var values = new Dictionary<string, string>
        {
            ["experimental.feature"] = "true",
        };
        var actions = new List<ConfigurationMigrationAction>();
        var operation = new ConfigurationDeprecateKeyOperation
        {
            Key = "experimental.feature",
        };

        operation.Apply(values, actions);

        await Assert.That(values.ContainsKey("experimental.feature")).IsFalse();
        await Assert.That(values["_deprecated.experimental.feature"]).IsEqualTo("true");
        await Assert.That(actions.Count).IsEqualTo(1);
        await Assert.That(actions[0].Kind).IsEqualTo(ConfigurationMigrationActionKind.Deprecated);
        await Assert.That(actions[0].Key).IsEqualTo("experimental.feature");
        await Assert.That(actions[0].SecondaryKey).IsEqualTo("_deprecated.experimental.feature");
        await Assert.That(actions[0].Value).IsEqualTo("true");
    }
}
