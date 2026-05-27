using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Proxyfan.Domain.Certificates;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking;
using Proxyfan.Framework.Platform;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace Proxyfan.DependencyInjection;

/// <summary>
///     Extension methods for <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />
///     that register types against every interface they implement.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the proxy listener services, including <see cref="IProxyListener" />, <see cref="ProxyOptions" />
    ///     binding, and options validation.
    ///     <para>
    ///         <strong>Prerequisite:</strong> An <see cref="Proxyfan.Domain.IDomainEventBus" /> singleton must already be
    ///         registered in <paramref name="serviceCollection" /> before calling this method.
    ///     </para>
    /// </summary>
    /// <param name="serviceCollection">The service collection to register services into.</param>
    /// <param name="configuration">The configuration used to bind <see cref="ProxyOptions" />.</param>
    /// <returns>The service collection, for chaining.</returns>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddProxyListener(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.Configure<ProxyOptions>(configuration.GetSection(ProxyOptions.SectionKey));
        serviceCollection.AddSingleton<IValidateOptions<ProxyOptions>, ProxyOptionsValidator>();
        serviceCollection.AddSingleton<ITrafficStore, TrafficStore>();
        serviceCollection.AddSingleton<ICertificateGenerator, RsaCertificateGenerator>();
        serviceCollection.AddSingleton<ICertificateStore, WindowsCertificateStore>();
        var serverNameIndicationProxyingList = new ServerNameIndicationProxyingList(isEnabled: true);
        serverNameIndicationProxyingList.AddIncludedPattern("*");
        serviceCollection.AddSingleton(serverNameIndicationProxyingList);
        serviceCollection.AddSingleton<TransportLayerSecurityInterceptionContext>();
        serviceCollection.AddSingleton<TransportLayerSecurityInterceptorHandlerDependencies>(BuildTransportLayerSecurityDependencies);
        serviceCollection.AddSingleton<HypertextTransferProtocolProxyHandlerDependencies>(BuildHypertextTransferProtocolDependencies);
        serviceCollection.AddSingleton<IConnectionHandler, HypertextTransferProtocolProxyHandler>();
        serviceCollection.AddSingleton<IConnectionHandler, TransportLayerSecurityInterceptorHandler>();
        serviceCollection.AddSingleton<IConnectionHandler, SocksTunnelHandler>();
        serviceCollection.AddSingleton<IProxyListener, SocketProxyListener>();
        serviceCollection.AddSingleton<ISystemProxy, WindowsSystemProxy>();
        serviceCollection.AddSingleton<IConnectionDispatcher, ConnectionDispatcher>();
        serviceCollection.AddSingleton<IRuleRegistry, RuleRegistry>();
        serviceCollection.AddSingleton(_ =>
        {
            var allowList = new MutableAllowListRule(priority: 50, isEnabled: false);
            return allowList;
        });
        serviceCollection.AddSingleton(_ =>
        {
            var blockList = new MutableBlockListRule(priority: 100, isEnabled: true);
            return blockList;
        });
        serviceCollection.AddSingleton<IRuleEngine>(provider =>
        {
            var registry = provider.GetRequiredService<IRuleRegistry>();
            var allowList = provider.GetRequiredService<MutableAllowListRule>();
            var blockList = provider.GetRequiredService<MutableBlockListRule>();
            registry.RegisterRequestPhaseRule(allowList);
            registry.RegisterRequestPhaseRule(blockList);
            return new RuleEngine(registry);
        });
        return serviceCollection;
    }

    /// <summary>
    ///     Registers <paramref name="implementation" /> as a singleton against every interface it implements,
    ///     excluding <see cref="IDisposable" />.
    /// </summary>
    /// <typeparam name="TImplementation">The concrete type of the implementation instance.</typeparam>
    /// <param name="serviceCollection">The service collection to register services into.</param>
    /// <param name="implementation">The factory that produces the singleton instance.</param>
    [RequiresUnreferencedCode("Scans implemented interfaces by reflection; not trim-safe by design.")]
    public static void AddSingletonAsImplementedInterfaces<TImplementation>(
        this IServiceCollection serviceCollection,
        ImplementationFactory<TImplementation> implementation)
        where TImplementation : notnull
    {
        var type = typeof(TImplementation);

        foreach (var @interface in type.GetInterfaces())
        {
            if (@interface != typeof(IDisposable))
            {
                var descriptor = new ServiceDescriptor(@interface, _ => implementation.Invoke(), ServiceLifetime.Singleton);
                serviceCollection.Add(descriptor);
            }
        }
    }

    private static HypertextTransferProtocolProxyHandlerDependencies BuildHypertextTransferProtocolDependencies(IServiceProvider provider)
    {
        var dependencies = new HypertextTransferProtocolProxyHandlerDependencies
        {
            TrafficStore = provider.GetRequiredService<ITrafficStore>(),
            EventBus = provider.GetRequiredService<Proxyfan.Domain.IDomainEventBus>(),
            RuleEngine = provider.GetRequiredService<IRuleEngine>(),
            Logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HypertextTransferProtocolProxyHandler>>(),
            UpstreamProxy = provider.GetService<IOptionsMonitor<UpstreamProxyOptions>>(),
            ThrottleProfile = provider.GetService<IOptionsMonitor<Proxyfan.Domain.Throttling.ThrottleProfile>>(),
            BreakpointHandler = provider.GetService<Proxyfan.Domain.Rules.Rules.IBreakpointHandler>(),
        };
        return dependencies;
    }

    private static TransportLayerSecurityInterceptorHandlerDependencies BuildTransportLayerSecurityDependencies(IServiceProvider provider)
    {
        var dependencies = new TransportLayerSecurityInterceptorHandlerDependencies
        {
            Context = provider.GetRequiredService<TransportLayerSecurityInterceptionContext>(),
            TrafficStore = provider.GetRequiredService<ITrafficStore>(),
            EventBus = provider.GetRequiredService<Proxyfan.Domain.IDomainEventBus>(),
            Logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TransportLayerSecurityInterceptorHandler>>(),
            RuleEngine = provider.GetService<IRuleEngine>(),
            BreakpointHandler = provider.GetService<Proxyfan.Domain.Rules.Rules.IBreakpointHandler>(),
        };
        return dependencies;
    }
}
