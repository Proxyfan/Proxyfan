using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using Proxyfan.Framework.Networking;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Architecture conformance tests for the Framework.Platform assembly.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(typeof(RsaCertificateGenerator).Assembly, typeof(ConnectionDispatcher).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that Framework.Platform does not depend on Framework.Networking.
    /// </summary>
    [Test]
    public void Classes_ResidingInFrameworkPlatform_ShouldNotDependOnFrameworkNetworking()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Framework.Platform")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Framework.Networking");

        TestArchitecture.CheckRule(rule);
    }
}