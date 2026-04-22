using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Proxyfan.Domain.Proxy;
using Proxyfan.Framework.Networking;

namespace Proxyfan.DependencyInjection;

/// <summary>
///     Extension methods for <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />
///     that register types against every interface they implement.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="serviceCollection">The service collection to register services into.</param>
    extension(IServiceCollection serviceCollection)
    {
        /// <summary>
        ///     Registers <paramref name="implementation" /> as a singleton against every interface it implements.
        /// </summary>
        /// <typeparam name="TImplementation">The concrete type of the implementation instance.</typeparam>
        /// <param name="implementation">The singleton instance to register.</param>
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Scans assembly types and implemented interfaces by reflection; not trim-safe by design.")]
        public void AddSingletonAsImplementedInterfaces<TImplementation>(Func<TImplementation> implementation)
            where TImplementation : notnull
        {
            var type = typeof(TImplementation);
            foreach (var @interface in type.GetTypeInfo().ImplementedInterfaces)
            {
                if (@interface != typeof(IDisposable))
                {
                    serviceCollection.Add(new ServiceDescriptor(@interface, _ => implementation.Invoke(), ServiceLifetime.Singleton));
                }
            }
        }

        /// <summary>
        ///     Registers the proxy listener services, including <see cref="IProxyListener" />, <see cref="ProxyOptions" />
        ///     binding,
        ///     and options validation.
        ///     <para>
        ///         <strong>Prerequisite:</strong> An <see cref="Proxyfan.Domain.IDomainEventBus" /> singleton must already be registered
        ///         in <paramref name="serviceCollection" /> before calling this method.
        ///     </para>
        /// </summary>
        /// <param name="configuration">The configuration used to bind <see cref="ProxyOptions" />.</param>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddProxyListener(IConfiguration configuration)
        {
            serviceCollection.Configure<ProxyOptions>(configuration.GetSection(ProxyOptions.SectionKey));
            serviceCollection.AddSingleton<IValidateOptions<ProxyOptions>, ProxyOptionsValidator>();
            serviceCollection.AddSingleton<IProxyListener, TcpProxyListener>();
            serviceCollection.AddSingleton<IConnectionDispatcher, ConnectionDispatcher>();
            return serviceCollection;
        }
    }
}