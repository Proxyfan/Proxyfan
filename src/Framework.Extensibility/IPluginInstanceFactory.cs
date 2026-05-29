namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Abstraction over the act of materialising a live plugin instance from
///     a discovered <see cref="PluginCandidate" />. The default implementation
///     (<see cref="IsolatedPluginInstanceFactory" />) uses a dedicated
///     <see cref="System.Runtime.Loader.AssemblyLoadContext" /> for isolation; tests can
///     supply lightweight stubs.
/// </summary>
public interface IPluginInstanceFactory
{
    /// <summary>
    ///     Creates a live plugin instance for the supplied candidate.
    /// </summary>
    /// <param name="candidate">The candidate to materialise. Must be valid.</param>
    /// <returns>The instantiation result.</returns>
    PluginInstantiationResult Create(PluginCandidate candidate);
}
