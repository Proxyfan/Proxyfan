using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Architecture conformance tests for the Domain.Traffic assembly.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(typeof(TrafficFlow).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that <c>Domain.Traffic</c> does not depend on any <c>Framework</c> module,
    ///     enforcing the domain-independence principle.
    /// </summary>
    [Test]
    public void Classes_ResidingInDomainTraffic_ShouldNotDependOnFramework()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Domain.Traffic")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Framework");

        TestArchitecture.CheckRule(rule);
    }

    /// <summary>
    ///     Verifies that <c>Domain.Traffic</c> does not depend on <c>Domain.Proxy</c>,
    ///     enforcing that the traffic bounded context is independent of the proxy module.
    /// </summary>
    [Test]
    public void Classes_ResidingInDomainTraffic_ShouldNotDependOnDomainProxy()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Domain.Traffic")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Domain.Proxy");

        TestArchitecture.CheckRule(rule);
    }
}