using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using Proxyfan.Domain.Rules.Matching;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Architecture conformance tests for the Domain.Rules assembly.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(typeof(MatchingRule).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that <c>Domain.Rules</c> does not depend on any <c>Framework</c> module,
    ///     enforcing the domain-independence principle.
    /// </summary>
    [Test]
    public void Classes_ResidingInDomainRules_ShouldNotDependOnFramework()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Domain.Rules")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Framework");

        TestArchitecture.CheckRule(rule);
    }

    /// <summary>
    ///     Verifies that <c>Domain.Rules</c> does not depend on any presentation namespace.
    /// </summary>
    [Test]
    public void Classes_ResidingInDomainRules_ShouldNotDependOnPresentation()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Domain.Rules")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Presentation");

        TestArchitecture.CheckRule(rule);
    }
}
