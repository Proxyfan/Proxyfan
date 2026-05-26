using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Har;

/// <summary>
///     Defines the contract for exporting captured traffic flows as an HTTP Archive (HAR) 1.2 document.
/// </summary>
public interface IHarExporter
{
    /// <summary>
    ///     Serializes the supplied traffic flows into the HAR 1.2 JSON format and writes the result
    ///     to the supplied output stream.
    /// </summary>
    /// <param name="flows">The captured traffic flows to export.</param>
    /// <param name="output">The output stream to write the HAR document to.</param>
    /// <param name="cancellationToken">A token that cancels the export.</param>
    /// <returns>A task that completes when the document has been written and flushed.</returns>
    Task ExportAsync(IReadOnlyList<TrafficFlow> flows, Stream output, CancellationToken cancellationToken);
}
