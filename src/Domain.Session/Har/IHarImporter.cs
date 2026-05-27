using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Har;

/// <summary>
///     Defines the contract for importing HAR 1.2 JSON back into <see cref="TrafficFlow" /> instances.
/// </summary>
public interface IHarImporter
{
    /// <summary>
    ///     Parses a HAR document from the supplied stream and returns the contained traffic flows.
    /// </summary>
    /// <param name="input">The HAR JSON input stream.</param>
    /// <param name="cancellationToken">A token that cancels the parse.</param>
    /// <returns>The list of parsed traffic flows.</returns>
    Task<IReadOnlyList<TrafficFlow>> ImportAsync(Stream input, CancellationToken cancellationToken);
}
