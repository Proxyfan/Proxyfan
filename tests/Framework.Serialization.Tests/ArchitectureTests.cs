using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Architecture conformance tests for the Framework.Serialization assembly.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(typeof(ContentEncodingDecoder).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that <c>Framework.Serialization</c> does not depend on any presentation namespace,
    ///     keeping the serialization layer independent of UI concerns.
    /// </summary>
    [Test]
    public void Classes_ResidingInFrameworkSerialization_ShouldNotDependOnPresentation()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Framework.Serialization")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Presentation");

        TestArchitecture.CheckRule(rule);
    }

    /// <summary>
    ///     Verifies that <c>Framework.Serialization</c> does not depend on <c>Framework.Networking</c>,
    ///     keeping the two framework modules independent of one another.
    /// </summary>
    [Test]
    public void Classes_ResidingInFrameworkSerialization_ShouldNotDependOnFrameworkNetworking()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Framework.Serialization")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Framework.Networking");

        TestArchitecture.CheckRule(rule);
    }
}
