using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Abstraction over the source of plugin update manifests. The default implementation
///     fetches a JSON document from a configurable URL via <see cref="System.Net.Http.HttpClient" />,
///     but tests substitute a stub that returns a literal manifest.
/// </summary>
public interface IPluginUpdateFeed
{
    /// <summary>
    ///     Fetches the manifest. Returns <see langword="null" /> when the feed is not
    ///     configured (empty URL), when the remote endpoint is unreachable, or when the
    ///     payload cannot be parsed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed manifest or <see langword="null" />.</returns>
    Task<PluginUpdateManifest?> FetchAsync(CancellationToken cancellationToken);
}
