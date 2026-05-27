using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Updates;

namespace Proxyfan.Domain.Updates.Tests;

/// <summary>
///     Tests for <see cref="UpdateChecker" />.
/// </summary>
public sealed class UpdateCheckerTests
{
    /// <summary>
    ///     Verifies CheckAsync returns the update when the feed reports a newer version.
    /// </summary>
    [Test]
    public async Task CheckAsync_FeedNewerVersion_ReturnsUpdate()
    {
        var info = new UpdateInfo
        {
            Version = "2.0.0",
            DownloadUrl = "https://example.com/proxyfan-2.0.0.msi",
            ReleaseNotes = "release notes",
        };
        UpdateFeedFunction feed = (_) => Task.FromResult<UpdateInfo?>(info);
        var checker = new UpdateChecker(feed);

        var result = await checker.CheckAsync("1.0.0", CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Version).IsEqualTo("2.0.0");
    }

    /// <summary>
    ///     Verifies CheckAsync returns null when the feed reports a same-or-older version.
    /// </summary>
    [Test]
    public async Task CheckAsync_FeedSameVersion_ReturnsNull()
    {
        var info = new UpdateInfo
        {
            Version = "1.0.0",
            DownloadUrl = "https://example.com/proxyfan-1.0.0.msi",
        };
        UpdateFeedFunction feed = (_) => Task.FromResult<UpdateInfo?>(info);
        var checker = new UpdateChecker(feed);

        var result = await checker.CheckAsync("1.0.0", CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies CheckAsync returns null when the feed reports no release.
    /// </summary>
    [Test]
    public async Task CheckAsync_FeedReturnsNull_ReturnsNull()
    {
        UpdateFeedFunction feed = (_) => Task.FromResult<UpdateInfo?>(null);
        var checker = new UpdateChecker(feed);

        var result = await checker.CheckAsync("1.0.0", CancellationToken.None);

        await Assert.That(result).IsNull();
    }
}
