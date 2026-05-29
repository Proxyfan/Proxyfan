using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Configuration.Migration;

namespace Proxyfan.Domain.Configuration.Tests.Migration;

/// <summary>
///     Tests for <see cref="ConfigurationRenameKeyOperation" />.
/// </summary>
public sealed class ConfigurationRenameKeyOperationTests
{
    /// <summary>
    ///     When the old key is absent the operation is a no-op.
    /// </summary>
    [Test]
    public async Task Apply_KeyAbsent_DoesNothing()
    {
        var values = new Dictionary<string, string>();
        var actions = new List<ConfigurationMigrationAction>();
        var operation = new ConfigurationRenameKeyOperation
        {
            NewKey = "proxy.port",
            OldKey = "proxy.listenPort",
        };

        operation.Apply(values, actions);

        await Assert.That(values.Count).IsEqualTo(0);
        await Assert.That(actions.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     When the old key is present the value moves to the new key and the action is recorded.
    /// </summary>
    [Test]
    public async Task Apply_KeyPresent_MovesValue()
    {
        var values = new Dictionary<string, string>
        {
            ["proxy.listenPort"] = "8080",
        };
        var actions = new List<ConfigurationMigrationAction>();
        var operation = new ConfigurationRenameKeyOperation
        {
            NewKey = "proxy.port",
            OldKey = "proxy.listenPort",
        };

        operation.Apply(values, actions);

        await Assert.That(values.ContainsKey("proxy.listenPort")).IsFalse();
        await Assert.That(values["proxy.port"]).IsEqualTo("8080");
        await Assert.That(actions.Count).IsEqualTo(1);
        await Assert.That(actions[0].Kind).IsEqualTo(ConfigurationMigrationActionKind.Renamed);
        await Assert.That(actions[0].Key).IsEqualTo("proxy.listenPort");
        await Assert.That(actions[0].SecondaryKey).IsEqualTo("proxy.port");
        await Assert.That(actions[0].Value).IsEqualTo("8080");
    }

    /// <summary>
    ///     When the new key already exists the existing value wins and the old key is removed.
    /// </summary>
    [Test]
    public async Task Apply_NewKeyAlreadyExists_PreservesExistingValue()
    {
        var values = new Dictionary<string, string>
        {
            ["proxy.listenPort"] = "8080",
            ["proxy.port"] = "9090",
        };
        var actions = new List<ConfigurationMigrationAction>();
        var operation = new ConfigurationRenameKeyOperation
        {
            NewKey = "proxy.port",
            OldKey = "proxy.listenPort",
        };

        operation.Apply(values, actions);

        await Assert.That(values.ContainsKey("proxy.listenPort")).IsFalse();
        await Assert.That(values["proxy.port"]).IsEqualTo("9090");
        await Assert.That(actions.Count).IsEqualTo(1);
    }
}
