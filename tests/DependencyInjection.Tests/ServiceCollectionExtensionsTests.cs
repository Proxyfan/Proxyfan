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

    [Test]
    public async Task AddSingletonAsImplementedInterfaces_WhenResolvedViaMultipleInterfaces_ReusesSameSingletonInstance()
    {
        var creationCount = 0;
        var services = new ServiceCollection();
        services.AddSingletonAsImplementedInterfaces<SampleImplementation>(() =>
        {
            creationCount++;
            return new SampleImplementation();
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IFirstContract>();
        var second = provider.GetRequiredService<ISecondContract>();

        await Assert.That(first).IsSameReferenceAs(second);
        await Assert.That(first).IsTypeOf<SampleImplementation>();
        await Assert.That(creationCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Resolving the singletons that are wired in by AddProxyListener forces the
    ///     DI factory lambdas to execute, covering the body of those registrations
    ///     (HttpClient, IComposerHistoryStore, IUserPreferencesStore, RemoteDeviceTracker,
    ///     IReverseProxyEngine alias).
    /// </summary>
    [Test]
    public async Task AddProxyListener_ResolvingFactoryRegistrations_ExercisesFactoryLambdas()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        var configuration = configurationBuilder.Build();
        services.AddLogging();
        services.AddSingleton<IDomainEventBus, DomainEventBus>();
        services.AddProxyListener(configuration);

        ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            var httpClient = provider.GetService<System.Net.Http.HttpClient>();
            var historyStore = provider.GetService<Proxyfan.Domain.Traffic.IComposerHistoryStore>();
            var preferencesStore = provider.GetService<Proxyfan.Domain.Configuration.IUserPreferencesStore>();
            var remoteDeviceTracker = provider.GetService<Proxyfan.Domain.RemoteDevices.RemoteDeviceTracker>();
            var reverseProxyEngine = provider.GetService<Proxyfan.Domain.Proxy.IReverseProxyEngine>();

            await Assert.That(httpClient).IsNotNull();
            await Assert.That(historyStore).IsNotNull();
            await Assert.That(preferencesStore).IsNotNull();
            await Assert.That(remoteDeviceTracker).IsNotNull();
            await Assert.That(reverseProxyEngine).IsNotNull();
        }
        finally
        {
            await provider.DisposeAsync();
        }
    }

    /// <summary>
    ///     Resolving the auto-update, plugin, and reverse-proxy health-checker singletons
    ///     forces the matching factory lambdas to execute, covering the
    ///     <c>&lt;AddAutoUpdate&gt;b__*</c>, <c>&lt;AddPlugins&gt;b__*</c>, and
    ///     <c>&lt;AddReverseProxy&gt;b__*</c> closures.
    /// </summary>
    [Test]
    public async Task AddProxyListener_ResolvingUpdatePluginReverseProxyFactories_ExercisesLambdas()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        var configuration = configurationBuilder.Build();
        services.AddLogging();
        services.AddSingleton<IDomainEventBus, DomainEventBus>();
        services.AddProxyListener(configuration);

        ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            var updateFeedFunction = provider.GetService<Proxyfan.Domain.Updates.UpdateFeedFunction>();
            var updateChecker = provider.GetService<Proxyfan.Domain.Updates.IUpdateChecker>();
            var enabledStateStore = provider.GetService<Proxyfan.Framework.Extensibility.IPluginEnabledStateStore>();
            var rootProvider = provider.GetService<Proxyfan.Framework.Extensibility.PluginRootDirectoryProvider>();
            var pluginHost = provider.GetService<Proxyfan.Plugin.Abstractions.IPluginHost>();
            var pluginUpdateManifestUrlProvider = provider.GetService<Proxyfan.Framework.Extensibility.PluginUpdateManifestUrlProvider>();
            var healthChecker = provider.GetService<Proxyfan.Domain.Proxy.PeriodicReverseProxyHealthChecker>();

            await Assert.That(updateFeedFunction).IsNotNull();
            await Assert.That(updateChecker).IsNotNull();
            await Assert.That(enabledStateStore).IsNotNull();
            await Assert.That(rootProvider).IsNotNull();
            await Assert.That(pluginHost).IsNotNull();
            await Assert.That(pluginUpdateManifestUrlProvider).IsNotNull();
            await Assert.That(healthChecker).IsNotNull();
        }
        finally
        {
            await provider.DisposeAsync();
        }
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