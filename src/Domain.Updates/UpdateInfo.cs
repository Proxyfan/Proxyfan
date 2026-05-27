namespace Proxyfan.Domain.Updates;

/// <summary>
///     Describes an available update returned by <see cref="IUpdateChecker" />.
/// </summary>
public sealed class UpdateInfo
{
    /// <summary>
    ///     Gets the URL where the update can be downloaded.
    /// </summary>
    public required string DownloadUrl { get; init; }

    /// <summary>
    ///     Gets the optional release notes summarizing changes since the current version.
    /// </summary>
    public string? ReleaseNotes { get; init; }

    /// <summary>
    ///     Gets the semantic version of the available update.
    /// </summary>
    public required string Version { get; init; }
}
