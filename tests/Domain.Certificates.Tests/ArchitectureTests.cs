using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using Proxyfan.Framework.Platform;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Architecture conformance tests for the Domain.Certificates assembly.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(typeof(CertificateAuthority).Assembly, typeof(RsaCertificateGenerator).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that Domain.Certificates does not depend on Framework.Platform.
    /// </summary>
    [Test]
    public void Classes_ResidingInDomainCertificates_ShouldNotDependOnFrameworkPlatform()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("Proxyfan.Domain.Certificates")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Proxyfan.Framework.Platform");

        TestArchitecture.CheckRule(rule);
    }
}