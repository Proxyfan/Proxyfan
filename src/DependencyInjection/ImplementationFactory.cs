namespace Proxyfan.DependencyInjection;

/// <summary>
///     A factory delegate that produces an instance of <typeparamref name="TImplementation" /> for
///     use in <see cref="ServiceCollectionExtensions.AddSingletonAsImplementedInterfaces{TImplementation}" />.
/// </summary>
/// <typeparam name="TImplementation">
///     The concrete type to produce. Must be a non-null reference or value type.
/// </typeparam>
/// <returns>
///     A new instance of <typeparamref name="TImplementation" />.
/// </returns>
public delegate TImplementation ImplementationFactory<out TImplementation>() where TImplementation : notnull;