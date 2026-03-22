using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Proxyfan.DependencyInjection;

/// <summary>
///     Extension methods for <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />
///     that register types against every interface they implement.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static void AddSingletonAsImplementedInterfaces<TImplementation>(IServiceCollection serviceCollection, Func<TImplementation> implementation, TypeWithInterfaces type)
        where TImplementation : notnull
    {
        foreach (var @interface in type.Interfaces)
        {
            serviceCollection.Add(new ServiceDescriptor(@interface, _ => implementation.Invoke(), ServiceLifetime.Singleton));
        }
    }

    private static IEnumerable<Type> GetTypeInterfaces(Type type)
    {
        foreach (var @interface in type.GetTypeInfo().ImplementedInterfaces)
        {
            if (@interface != typeof(IDisposable))
            {
                yield return @interface;
            }
        }
    }

    extension(IServiceCollection serviceCollection)
    {
        /// <summary>
        ///     Registers <paramref name="implementation" /> as a singleton against every interface it implements.
        /// </summary>
        /// <typeparam name="TImplementation">The concrete type of the implementation instance.</typeparam>
        /// <param name="implementation">The singleton instance to register.</param>
        [RequiresUnreferencedCode("Scans assembly types and implemented interfaces by reflection; not trim-safe by design.")]
        public void AddSingletonAsImplementedInterfaces<TImplementation>(Func<TImplementation> implementation)
            where TImplementation : notnull
        {
            var type = typeof(TImplementation);
            var interfaces = GetTypeInterfaces(type);
            var typeWithInterfaces = new TypeWithInterfaces(type, interfaces);
            AddSingletonAsImplementedInterfaces(serviceCollection, implementation, typeWithInterfaces);
        }
    }

    private sealed record TypeWithInterfaces(Type Type, IEnumerable<Type> Interfaces);
}