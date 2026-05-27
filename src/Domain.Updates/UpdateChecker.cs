using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Updates;

/// <summary>
///     Default <see cref="IUpdateChecker" /> that consults a supplied feed function (which is
///     pluggable so production code can use GitHub Releases while tests can use a stub).
///     The checker returns the latest version reported by the feed only when it is newer
///     than the supplied current version, as judged by <see cref="SemanticVersionComparer" />.
/// </summary>
public sealed class UpdateChecker : IUpdateChecker
{
    private readonly UpdateFeedFunction _feed;

    /// <summary>
    ///     Initializes a new <see cref="UpdateChecker" /> with the supplied feed function.
    /// </summary>
    /// <param name="feed">Function that returns the latest available release.</param>
    public UpdateChecker(UpdateFeedFunction feed)
    {
        _feed = feed;
    }

    /// <inheritdoc />
    public async Task<UpdateInfo?> CheckAsync(string currentVersion, CancellationToken cancellationToken)
    {
        var latest = await _feed.Invoke(cancellationToken).ConfigureAwait(false);

        if (latest is null)
        {
            return null;
        }

        if (!SemanticVersionComparer.HasNewerVersion(currentVersion, latest.Version))
        {
            return null;
        }

        return latest;
    }
}
