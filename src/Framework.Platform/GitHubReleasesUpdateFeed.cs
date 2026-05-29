using Proxyfan.Domain.Updates;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Adapts the GitHub Releases REST API into an <see cref="UpdateFeedFunction" /> suitable
///     for use by <see cref="Proxyfan.Domain.Updates.UpdateChecker" />. The returned function
///     fetches <c>/repos/{owner}/{repo}/releases/latest</c> and translates the JSON response
///     into <see cref="UpdateInfo" />.
/// </summary>
public static class GitHubReleasesUpdateFeed
{
    /// <summary>
    ///     Builds an update feed that consults the GitHub Releases REST API for the supplied
    ///     repository.
    /// </summary>
    /// <param name="hypertextTransferProtocolClient">The HTTP client to use for requests.</param>
    /// <param name="owner">The GitHub owner (user/org) name.</param>
    /// <param name="repository">The GitHub repository name.</param>
    /// <returns>An <see cref="UpdateFeedFunction" /> that fetches the latest release.</returns>
    public static UpdateFeedFunction Create(HttpClient hypertextTransferProtocolClient, string owner, string repository)
    {
        async Task<UpdateInfo?> Fetch(CancellationToken cancellationToken)
        {
            var url = $"https://api.github.com/repos/{owner}/{repository}/releases/latest";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Proxyfan/1.0 (+https://github.com/Proxyfan/Proxyfan)");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            using var hypertextTransferProtocolResponse = await hypertextTransferProtocolClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!hypertextTransferProtocolResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var response = await hypertextTransferProtocolResponse.Content.ReadFromJsonAsync<GitHubReleaseResponse>(cancellationToken).ConfigureAwait(false);

            if (response is null)
            {
                return null;
            }

            return ConvertToUpdateInfo(response);
        }

        return Fetch;
    }

    private static UpdateInfo? ConvertToUpdateInfo(GitHubReleaseResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.TagName))
        {
            return null;
        }

        var version = response.TagName.StartsWith('v') ? response.TagName[1..] : response.TagName;
        var downloadUrl = response.HypertextTransferProtocolUrl ?? string.Empty;
        var info = new UpdateInfo
        {
            Version = version,
            DownloadUrl = downloadUrl,
            ReleaseNotes = response.Body,
        };
        return info;
    }
}
