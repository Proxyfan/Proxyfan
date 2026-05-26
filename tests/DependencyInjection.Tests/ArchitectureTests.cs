using Architecture = ArchUnitNET.Domain.Architecture;
using ArchUnitNET.Loader;
using Proxyfan.DependencyInjection;
using Proxyfan.Domain;
using Proxyfan.Domain.Certificates;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking;
using Proxyfan.Framework.Platform;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Proxyfan.DependencyInjection.Tests;

/// <summary>
///     Architecture tests for the DependencyInjection assembly.
///     Verifies its expected project references and one-way dependency direction.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture TestArchitecture;

    static ArchitectureTests()
    {
        var loader = new ArchLoader();
        TestArchitecture = loader
            .LoadAssemblies(typeof(ServiceCollectionExtensions).Assembly)
            .Build();
    }

    /// <summary>
    ///     Verifies that the DependencyInjection assembly only references the expected project assemblies.
    ///     This keeps the composition root limited to the current dependency surface.
    /// </summary>
    [Test]
    public async Task DependencyInjectionAssembly_WhenLoaded_OnlyReferencesExpectedProjectAssemblies()
    {
        var referencedAssemblyNames = typeof(ServiceCollectionExtensions).Assembly
            .GetReferencedAssemblies()
            .Select(static assemblyName => assemblyName.Name)
            .OfType<string>()
            .Where(HasProjectAssemblyName)
            .ToArray();

        await Assert.That(TestArchitecture).IsNotNull();
        await Assert.That(referencedAssemblyNames).Count().IsEqualTo(5);
        await Assert.That(referencedAssemblyNames).Contains("Domain.Certificates");
        await Assert.That(referencedAssemblyNames).Contains("Domain.Proxy");
        await Assert.That(referencedAssemblyNames).Contains("Domain.Traffic");
        await Assert.That(referencedAssemblyNames).Contains("Framework.Networking");
        await Assert.That(referencedAssemblyNames).Contains("Framework.Platform");
    }

    /// <summary>
    ///     Verifies that the Domain kernel does not reference DependencyInjection.
    ///     This preserves the composition root as an outer-layer concern.
    /// </summary>
    [Test]
    public async Task DomainAssembly_WhenLoaded_DoesNotReferenceDependencyInjectionAssembly()
    {
        var hasDependency = HasDependencyOnDependencyInjectionAssembly(typeof(DomainEventBus).Assembly);
        await Assert.That(hasDependency).IsFalse();
    }

    /// <summary>
    ///     Verifies that Domain.Certificates does not reference DependencyInjection.
    ///     This preserves the composition root as an outer-layer concern.
    /// </summary>
    [Test]
    public async Task DomainCertificatesAssembly_WhenLoaded_DoesNotReferenceDependencyInjectionAssembly()
    {
        var hasDependency = HasDependencyOnDependencyInjectionAssembly(typeof(CertificateAuthority).Assembly);
        await Assert.That(hasDependency).IsFalse();
    }

    /// <summary>
    ///     Verifies that Domain.Proxy does not reference DependencyInjection.
    ///     This preserves the composition root as an outer-layer concern.
    /// </summary>
    [Test]
    public async Task DomainProxyAssembly_WhenLoaded_DoesNotReferenceDependencyInjectionAssembly()
    {
        var hasDependency = HasDependencyOnDependencyInjectionAssembly(typeof(IProxyListener).Assembly);
        await Assert.That(hasDependency).IsFalse();
    }

    /// <summary>
    ///     Verifies that Domain.Traffic does not reference DependencyInjection.
    ///     This preserves the composition root as an outer-layer concern.
    /// </summary>
    [Test]
    public async Task DomainTrafficAssembly_WhenLoaded_DoesNotReferenceDependencyInjectionAssembly()
    {
        var hasDependency = HasDependencyOnDependencyInjectionAssembly(typeof(ITrafficStore).Assembly);
        await Assert.That(hasDependency).IsFalse();
    }

    /// <summary>
    ///     Verifies that Framework.Networking does not reference DependencyInjection.
    ///     This preserves the composition root as an outer-layer concern.
    /// </summary>
    [Test]
    public async Task FrameworkNetworkingAssembly_WhenLoaded_DoesNotReferenceDependencyInjectionAssembly()
    {
        var hasDependency = HasDependencyOnDependencyInjectionAssembly(typeof(ConnectionDispatcher).Assembly);
        await Assert.That(hasDependency).IsFalse();
    }

    /// <summary>
    ///     Verifies that Framework.Platform does not reference DependencyInjection.
    ///     This preserves the composition root as an outer-layer concern.
    /// </summary>
    [Test]
    public async Task FrameworkPlatformAssembly_WhenLoaded_DoesNotReferenceDependencyInjectionAssembly()
    {
        var hasDependency = HasDependencyOnDependencyInjectionAssembly(typeof(WindowsSystemProxy).Assembly);
        await Assert.That(hasDependency).IsFalse();
    }

    private static bool HasDependencyOnDependencyInjectionAssembly(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Any(static assemblyName => assemblyName.Name == "DependencyInjection");
    }

    private static bool HasProjectAssemblyName(string assemblyName)
    {
        return assemblyName.StartsWith("Domain", StringComparison.Ordinal)
            || assemblyName.StartsWith("Framework", StringComparison.Ordinal);
    }
}