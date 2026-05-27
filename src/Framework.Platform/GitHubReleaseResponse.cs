using System.Text.Json.Serialization;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Subset of the GitHub releases JSON response used by
///     <see cref="GitHubReleasesUpdateFeed" />.
/// </summary>
public sealed class GitHubReleaseResponse
{
    /// <summary>
    ///     Gets the optional release body (markdown release notes).
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; init; }

    /// <summary>
    ///     Gets the HTML URL pointing to the release page on github.com.
    /// </summary>
    [JsonPropertyName("html_url")]
    public string? HypertextTransferProtocolUrl { get; init; }

    /// <summary>
    ///     Gets the tag name of the release (e.g. <c>v1.2.3</c>).
    /// </summary>
    [JsonPropertyName("tag_name")]
    public string? TagName { get; init; }
}
