using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Configuration.Migration;

namespace Proxyfan.Domain.Configuration.Tests.Migration;

/// <summary>
///     Tests for <see cref="ConfigurationAddDefaultOperation" />.
/// </summary>
public sealed class ConfigurationAddDefaultOperationTests
{
    /// <summary>
    ///     When the key is absent the default value is inserted and an action is recorded.
    /// </summary>
    [Test]
    public async Task Apply_KeyAbsent_InsertsDefault()
    {
        var values = new Dictionary<string, string>();
        var actions = new List<ConfigurationMigrationAction>();
        var operation = new ConfigurationAddDefaultOperation
        {
            DefaultValue = "5000",
            Key = "session.autoSaveInterval",
        };

        operation.Apply(values, actions);

        await Assert.That(values["session.autoSaveInterval"]).IsEqualTo("5000");
        await Assert.That(actions.Count).IsEqualTo(1);
        await Assert.That(actions[0].Kind).IsEqualTo(ConfigurationMigrationActionKind.DefaultAdded);
        await Assert.That(actions[0].Key).IsEqualTo("session.autoSaveInterval");
        await Assert.That(actions[0].Value).IsEqualTo("5000");
    }

    /// <summary>
    ///     When the key already exists the existing user value is preserved and no action
    ///     is recorded.
    /// </summary>
    [Test]
    public async Task Apply_KeyPresent_PreservesExistingValue()
    {
        var values = new Dictionary<string, string>
        {
            ["session.autoSaveInterval"] = "1234",
        };
        var actions = new List<ConfigurationMigrationAction>();
        var operation = new ConfigurationAddDefaultOperation
        {
            DefaultValue = "5000",
            Key = "session.autoSaveInterval",
        };

        operation.Apply(values, actions);

        await Assert.That(values["session.autoSaveInterval"]).IsEqualTo("1234");
        await Assert.That(actions.Count).IsEqualTo(0);
    }
}
