using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Architecture conformance tests for the Framework.Networking assembly.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(typeof(ConnectionDispatcher).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that <c>Framework.Networking</c> does not depend on any presentation namespace,
    ///     keeping the networking layer independent of UI concerns.
    /// </summary>
    [Test]
    public void Classes_ResidingInFrameworkNetworking_ShouldNotDependOnPresentation()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Framework.Networking")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Presentation");

        TestArchitecture.CheckRule(rule);
    }
}
