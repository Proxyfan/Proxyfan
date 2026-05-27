using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Updates;

/// <summary>
///     Checks for new Proxyfan releases. Implementations may consult a remote feed (e.g.
///     GitHub Releases) or be replaced with stubs in tests.
/// </summary>
public interface IUpdateChecker
{
    /// <summary>
    ///     Checks whether a release newer than the current version is available.
    /// </summary>
    /// <param name="currentVersion">The semantic version currently running.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The available update info, or null when no update is available.</returns>
    Task<UpdateInfo?> CheckAsync(string currentVersion, CancellationToken cancellationToken);
}
