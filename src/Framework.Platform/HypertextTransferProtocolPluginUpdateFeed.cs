using Proxyfan.Framework.Extensibility;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     <see cref="IPluginUpdateFeed" /> implementation that fetches the manifest JSON via
///     an <see cref="HttpClient" />. Returns <see langword="null" /> when the manifest URL
///     is empty (feature disabled), the request fails for any reason, the payload is
///     malformed, or the payload exceeds <see cref="ManifestSizeLimitInBytes" />.
/// </summary>
public sealed class HypertextTransferProtocolPluginUpdateFeed : IPluginUpdateFeed
{
    /// <summary>
    ///     Upper bound on the manifest payload size, in bytes. A compromised or misconfigured
    ///     manifest endpoint must not be able to force the client to buffer an arbitrarily large
    ///     response. One megabyte is well above the size of any realistic plugin index while
    ///     keeping memory pressure bounded.
    /// </summary>
    public const int ManifestSizeLimitInBytes = 1024 * 1024;
    private readonly HttpClient _hypertextTransferProtocolClient;
    private readonly PluginUpdateManifestUrlProvider _urlProvider;

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolPluginUpdateFeed" />.
    /// </summary>
    /// <param name="hypertextTransferProtocolClient">The HTTP client used for outbound requests.</param>
    /// <param name="urlProvider">The manifest URL provider.</param>
    public HypertextTransferProtocolPluginUpdateFeed(HttpClient hypertextTransferProtocolClient, PluginUpdateManifestUrlProvider urlProvider)
    {
        _hypertextTransferProtocolClient = hypertextTransferProtocolClient;
        _urlProvider = urlProvider;
    }

    /// <inheritdoc />
    public async Task<PluginUpdateManifest?> FetchAsync(CancellationToken cancellationToken)
    {
        if (!_urlProvider.HasConfiguration())
        {
            return null;
        }

        var manifestUrl = _urlProvider.GetManifestUrl();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, manifestUrl);
            request.Headers.UserAgent.ParseAdd("Proxyfan/1.0 (+https://github.com/Proxyfan/Proxyfan)");
            request.Headers.Accept.ParseAdd("application/json");
            using var response = await _hypertextTransferProtocolClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            if (response.Content.Headers.ContentLength is { } advertisedLength && advertisedLength > ManifestSizeLimitInBytes)
            {
                return null;
            }

            var payload = await BoundedHypertextTransferProtocolPayloadReader.ReadAsync(response.Content, ManifestSizeLimitInBytes, cancellationToken).ConfigureAwait(false);
            if (payload is null)
            {
                return null;
            }

            return PluginUpdateManifestParser.TryParse(payload);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
