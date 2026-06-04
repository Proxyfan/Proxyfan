using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Architecture conformance tests for the Domain.Scripting assembly.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(typeof(IScriptingHandler).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that <c>Domain.Scripting</c> does not depend on any <c>Framework</c> module,
    ///     enforcing the domain-independence principle.
    /// </summary>
    [Test]
    public void Classes_ResidingInDomainScripting_ShouldNotDependOnFramework()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Domain.Scripting")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Framework");

        TestArchitecture.CheckRule(rule);
    }

    /// <summary>
    ///     Verifies that <c>Domain.Scripting</c> does not depend on any presentation namespace.
    /// </summary>
    [Test]
    public void Classes_ResidingInDomainScripting_ShouldNotDependOnPresentation()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Domain.Scripting")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Presentation");

        TestArchitecture.CheckRule(rule);
    }
}
