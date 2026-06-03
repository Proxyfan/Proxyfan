using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Configuration.Migration;

namespace Proxyfan.Domain.Configuration.Tests.Migration;

/// <summary>
///     Tests for <see cref="ConfigurationMigrationPipeline" />.
/// </summary>
public sealed class ConfigurationMigrationPipelineTests
{
    /// <summary>
    ///     Duplicate migrators for the same source version are rejected up-front.
    /// </summary>
    [Test]
    public async Task Construction_DuplicateFromVersion_Throws()
    {
        var first = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations = [],
            To = new ConfigurationVersion(1, 1),
        };
        var second = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations = [],
            To = new ConfigurationVersion(2, 0),
        };

        await Assert.That(() => new ConfigurationMigrationPipeline([first, second]))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     When the source version already matches (or exceeds) the target the pipeline
    ///     returns a no-op result that still stamps the target version into the values.
    /// </summary>
    [Test]
    public async Task Migrate_SourceAtTarget_ReturnsNoopButStampsVersion()
    {
        var pipeline = new ConfigurationMigrationPipeline([]);
        var source = new Dictionary<string, string>
        {
            ["proxy.port"] = "8080",
            ["version"] = "2.0",
        };
        var target = new ConfigurationVersion(2, 0);

        var result = pipeline.Migrate(source, target);

        await Assert.That(result.IsMigrated).IsFalse();
        await Assert.That(result.SourceVersion).IsEqualTo(target);
        await Assert.That(result.TargetVersion).IsEqualTo(target);
        await Assert.That(result.Values["proxy.port"]).IsEqualTo("8080");
        await Assert.That(result.Values["version"]).IsEqualTo("2.0");
        await Assert.That(result.Actions.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     A missing version key is treated as <c>1.0</c> for the purposes of pipeline routing.
    /// </summary>
    [Test]
    public async Task Migrate_MissingVersionKey_AssumesVersionOnePointZero()
    {
        var migrator = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations =
            [
                new ConfigurationAddDefaultOperation
                {
                    DefaultValue = "dark",
                    Key = "ui.theme",
                },
            ],
            To = new ConfigurationVersion(1, 1),
        };
        var pipeline = new ConfigurationMigrationPipeline([migrator]);
        var source = new Dictionary<string, string>
        {
            ["proxy.port"] = "8080",
        };

        var result = pipeline.Migrate(source, new ConfigurationVersion(1, 1));

        await Assert.That(result.IsMigrated).IsTrue();
        await Assert.That(result.SourceVersion).IsEqualTo(new ConfigurationVersion(1, 0));
        await Assert.That(result.TargetVersion).IsEqualTo(new ConfigurationVersion(1, 1));
        await Assert.That(result.Values["ui.theme"]).IsEqualTo("dark");
        await Assert.That(result.Values["version"]).IsEqualTo("1.1");
    }

    /// <summary>
    ///     A multi-step migration chains migrators in ascending version order and aggregates
    ///     every action.
    /// </summary>
    [Test]
    public async Task Migrate_MultiStepPath_RunsAllMigratorsInOrder()
    {
        var step1 = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations =
            [
                new ConfigurationRenameKeyOperation
                {
                    NewKey = "proxy.port",
                    OldKey = "proxy.listenPort",
                },
            ],
            To = new ConfigurationVersion(1, 1),
        };
        var step2 = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 1),
            Operations =
            [
                new ConfigurationDeprecateKeyOperation
                {
                    Key = "experimental.feature",
                },
            ],
            To = new ConfigurationVersion(2, 0),
        };
        var pipeline = new ConfigurationMigrationPipeline([step1, step2]);
        var source = new Dictionary<string, string>
        {
            ["experimental.feature"] = "true",
            ["proxy.listenPort"] = "8080",
            ["version"] = "1.0",
        };

        var result = pipeline.Migrate(source, new ConfigurationVersion(2, 0));

        await Assert.That(result.IsMigrated).IsTrue();
        await Assert.That(result.SourceVersion).IsEqualTo(new ConfigurationVersion(1, 0));
        await Assert.That(result.TargetVersion).IsEqualTo(new ConfigurationVersion(2, 0));
        await Assert.That(result.Values["proxy.port"]).IsEqualTo("8080");
        await Assert.That(result.Values.ContainsKey("proxy.listenPort")).IsFalse();
        await Assert.That(result.Values["_deprecated.experimental.feature"]).IsEqualTo("true");
        await Assert.That(result.Values["version"]).IsEqualTo("2.0");
        await Assert.That(result.Actions.Count).IsEqualTo(4);
        await Assert.That(result.Actions[0].Kind).IsEqualTo(ConfigurationMigrationActionKind.Renamed);
        await Assert.That(result.Actions[1].Kind).IsEqualTo(ConfigurationMigrationActionKind.VersionBumped);
        await Assert.That(result.Actions[2].Kind).IsEqualTo(ConfigurationMigrationActionKind.Deprecated);
        await Assert.That(result.Actions[3].Kind).IsEqualTo(ConfigurationMigrationActionKind.VersionBumped);
    }

    /// <summary>
    ///     When the chain breaks (no migrator covers the current version) the pipeline throws.
    /// </summary>
    [Test]
    public async Task Migrate_MissingLink_Throws()
    {
        var step = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(2, 0),
            Operations = [],
            To = new ConfigurationVersion(2, 1),
        };
        var pipeline = new ConfigurationMigrationPipeline([step]);
        var source = new Dictionary<string, string>
        {
            ["version"] = "1.0",
        };

        await Assert.That(() => pipeline.Migrate(source, new ConfigurationVersion(2, 1)))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     A migrator must advance to a strictly greater version.
    /// </summary>
    [Test]
    public async Task Migrate_NonAdvancingTransition_Throws()
    {
        var step = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations = [],
            To = new ConfigurationVersion(1, 0),
        };
        var pipeline = new ConfigurationMigrationPipeline([step]);
        var source = new Dictionary<string, string>
        {
            ["version"] = "1.0",
        };

        await Assert.That(() => pipeline.Migrate(source, new ConfigurationVersion(1, 1)))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     A migrator cannot jump beyond the requested target version.
    /// </summary>
    [Test]
    public async Task Migrate_OvershootTransition_Throws()
    {
        var step = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations = [],
            To = new ConfigurationVersion(2, 0),
        };
        var pipeline = new ConfigurationMigrationPipeline([step]);
        var source = new Dictionary<string, string>
        {
            ["version"] = "1.0",
        };

        await Assert.That(() => pipeline.Migrate(source, new ConfigurationVersion(1, 1)))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     The pipeline does not mutate the supplied source dictionary.
    /// </summary>
    [Test]
    public async Task Migrate_AnyInput_DoesNotMutateSource()
    {
        var step = new ConfigurationMigrator
        {
            From = new ConfigurationVersion(1, 0),
            Operations =
            [
                new ConfigurationAddDefaultOperation
                {
                    DefaultValue = "system",
                    Key = "ui.theme",
                },
            ],
            To = new ConfigurationVersion(1, 1),
        };
        var pipeline = new ConfigurationMigrationPipeline([step]);
        var source = new Dictionary<string, string>
        {
            ["version"] = "1.0",
        };

        _ = pipeline.Migrate(source, new ConfigurationVersion(1, 1));

        await Assert.That(source.Count).IsEqualTo(1);
        await Assert.That(source.ContainsKey("ui.theme")).IsFalse();
    }
}
