using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Updates;

/// <summary>
///     Delegate used by <see cref="UpdateChecker" /> to fetch the latest published release
///     from an arbitrary feed source (e.g. GitHub Releases REST API, a private mirror, or a
///     test stub).
/// </summary>
/// <param name="cancellationToken">Cancels the fetch.</param>
/// <returns>The latest release, or null when none is available.</returns>
public delegate Task<UpdateInfo?> UpdateFeedFunction(CancellationToken cancellationToken);
