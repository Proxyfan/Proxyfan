using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proxyfan.DependencyInjection;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.DependencyInjection.Tests;

/// <summary>
///     Tests for <see cref="ServiceCollectionExtensions" />.
///     Covers service registration behavior for proxy infrastructure and interface scanning.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    ///     Verifies that <see cref="ServiceCollectionExtensions.AddProxyListener" /> registers the core proxy services.
    ///     Also verifies that the required connection handlers are available from the built provider.
    /// </summary>
    [Test]
    public async Task AddProxyListener_WhenCalled_RegistersRequiredServices()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        var configuration = configurationBuilder.Build();
        services.AddLogging();
        services.AddSingleton<IDomainEventBus, DomainEventBus>();
        services.AddProxyListener(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        var listener = provider.GetService<IProxyListener>();
        var dispatcher = provider.GetService<IConnectionDispatcher>();
        var trafficStore = provider.GetService<ITrafficStore>();
        var systemProxy = provider.GetService<ISystemProxy>();
        var ruleEngine = provider.GetService<IRuleEngine>();
        var handlers = provider.GetServices<IConnectionHandler>().ToArray();

        await Assert.That(listener).IsNotNull();
        await Assert.That(dispatcher).IsNotNull();
        await Assert.That(trafficStore).IsNotNull();
        await Assert.That(systemProxy).IsNotNull();
        await Assert.That(ruleEngine).IsNotNull();
        await Assert.That(handlers).Count().IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that <see cref="ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces{TImplementation}" />
    ///     registers every implemented interface except <see cref="IDisposable" />.
    /// </summary>
    [Test]
    public async Task AddSingletonAsImplementedInterfaces_WhenImplementationHasInterfaces_RegistersAllNonDisposableInterfaces()
    {
        var services = new ServiceCollection();
        services.AddSingletonAsImplementedInterfaces<SampleImplementation>(CreateSampleImplementation);

        using ServiceProvider provider = services.BuildServiceProvider();
        var firstRegistrationCount = services.Count(descriptor => descriptor.ServiceType == typeof(IFirstContract));
        var secondRegistrationCount = services.Count(descriptor => descriptor.ServiceType == typeof(ISecondContract));
        var disposableRegistrationCount = services.Count(descriptor => descriptor.ServiceType == typeof(IDisposable));
        var first = provider.GetService<IFirstContract>();
        var second = provider.GetService<ISecondContract>();
        var disposable = provider.GetService<IDisposable>();

        await Assert.That(firstRegistrationCount).IsEqualTo(1);
        await Assert.That(secondRegistrationCount).IsEqualTo(1);
        await Assert.That(disposableRegistrationCount).IsEqualTo(0);
        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNotNull();
        await Assert.That(disposable).IsNull();
    }

    private static SampleImplementation CreateSampleImplementation()
    {
        var implementation = new SampleImplementation();
        return implementation;
    }

    private interface IFirstContract
    {
    }

    private interface ISecondContract
    {
    }

    private sealed class SampleImplementation : IFirstContract, ISecondContract, IDisposable
    {
        public void Dispose()
        {
        }
    }
}