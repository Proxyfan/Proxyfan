using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using Proxyfan.Framework.Networking;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Architecture conformance tests for the Domain.Proxy assembly.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(
                typeof(IDomainEvent).Assembly,
                typeof(IProxyListener).Assembly,
                typeof(SocketProxyListener).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that classes in the <c>Proxyfan.Domain</c> kernel do not depend on
    ///     anything in <c>Proxyfan.Domain.Proxy</c>, enforcing the kernel independence rule.
    /// </summary>
    [Test]
    public void Classes_ResidingInDomainKernel_ShouldNotDependOnDomainProxy()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Domain")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Domain.Proxy");

        TestArchitecture.CheckRule(rule);
    }

    /// <summary>
    ///     Verifies that classes in <c>Proxyfan.Domain.Proxy</c> do not depend on
    ///     anything in <c>Proxyfan.Framework.Networking</c>.
    /// </summary>
    [Test]
    public void Classes_ResidingInDomainProxy_ShouldNotDependOnFrameworkNetworking()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Domain.Proxy")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Framework.Networking");

        TestArchitecture.CheckRule(rule);
    }
}