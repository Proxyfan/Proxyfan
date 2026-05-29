using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Configuration.Migration;

namespace Proxyfan.Domain.Configuration.Tests.Migration;

/// <summary>
///     Tests for <see cref="ConfigurationMigrator" />.
/// </summary>
public sealed class ConfigurationMigratorTests
{
    /// <summary>
    ///     The migrator applies its operations in order then appends a <see cref="ConfigurationMigrationActionKind.VersionBumped" />
    ///     action carrying the migrator's <see cref="IConfigurationMigrator.To" /> version.
    /// </summary>
    [Test]
    public async Task Apply_HasOperations_RunsThemInOrderThenBumpsVersion()
    {
        var migrator = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations =
            [
                new ConfigurationRenameKeyOperation
                {
                    NewKey = "proxy.port",
                    OldKey = "proxy.listenPort",
                },
                new ConfigurationAddDefaultOperation
                {
                    DefaultValue = "system",
                    Key = "ui.theme",
                },
            ],
            To = new ConfigurationVersion(2, 0),
        };
        var source = new Dictionary<string, string>
        {
            ["proxy.listenPort"] = "8080",
        };

        var result = migrator.Apply(source);

        await Assert.That(result.Values["proxy.port"]).IsEqualTo("8080");
        await Assert.That(result.Values.ContainsKey("proxy.listenPort")).IsFalse();
        await Assert.That(result.Values["ui.theme"]).IsEqualTo("system");
        await Assert.That(result.Values["version"]).IsEqualTo("2.0");
        await Assert.That(result.Actions.Count).IsEqualTo(3);
        await Assert.That(result.Actions[0].Kind).IsEqualTo(ConfigurationMigrationActionKind.Renamed);
        await Assert.That(result.Actions[1].Kind).IsEqualTo(ConfigurationMigrationActionKind.DefaultAdded);
        await Assert.That(result.Actions[2].Kind).IsEqualTo(ConfigurationMigrationActionKind.VersionBumped);
        await Assert.That(result.Actions[2].Key).IsEqualTo("version");
        await Assert.That(result.Actions[2].Value).IsEqualTo("2.0");
    }

    /// <summary>
    ///     With zero operations the migrator still bumps the version key.
    /// </summary>
    [Test]
    public async Task Apply_NoOperations_StillBumpsVersion()
    {
        var migrator = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations = [],
            To = new ConfigurationVersion(1, 1),
        };
        var source = new Dictionary<string, string>
        {
            ["proxy.port"] = "8080",
        };

        var result = migrator.Apply(source);

        await Assert.That(result.Values["proxy.port"]).IsEqualTo("8080");
        await Assert.That(result.Values["version"]).IsEqualTo("1.1");
        await Assert.That(result.Actions.Count).IsEqualTo(1);
        await Assert.That(result.Actions[0].Kind).IsEqualTo(ConfigurationMigrationActionKind.VersionBumped);
    }

    /// <summary>
    ///     The migrator does not mutate the supplied source dictionary.
    /// </summary>
    [Test]
    public async Task Apply_AnyInput_DoesNotMutateSource()
    {
        var migrator = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations =
            [
                new ConfigurationDeprecateKeyOperation
                {
                    Key = "experimental.feature",
                },
            ],
            To = new ConfigurationVersion(1, 1),
        };
        var source = new Dictionary<string, string>
        {
            ["experimental.feature"] = "true",
        };

        _ = migrator.Apply(source);

        await Assert.That(source.Count).IsEqualTo(1);
        await Assert.That(source["experimental.feature"]).IsEqualTo("true");
    }
}
