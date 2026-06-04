using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Architecture conformance tests for the Cli assembly.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(typeof(CliRunner).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that <c>Cli</c> does not depend on any presentation namespace,
    ///     keeping the CLI entry point free of UI-layer concerns.
    /// </summary>
    [Test]
    public void Classes_ResidingInCli_ShouldNotDependOnPresentation()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Cli")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Presentation");

        TestArchitecture.CheckRule(rule);
    }
}
