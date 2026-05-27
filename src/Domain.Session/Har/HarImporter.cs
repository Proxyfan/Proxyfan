using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Har;

/// <summary>
///     Default <see cref="IHarImporter" /> implementation that parses HAR 1.2 JSON documents
///     into <see cref="TrafficFlow" /> instances.
/// </summary>
public sealed class HarImporter : IHarImporter
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TrafficFlow>> ImportAsync(Stream input, CancellationToken cancellationToken)
    {
        var document = await JsonDocument.ParseAsync(input, cancellationToken: cancellationToken).ConfigureAwait(false);
        var flows = new List<TrafficFlow>();

        if (!document.RootElement.TryGetProperty("log", out var log))
        {
            return flows;
        }

        if (!log.TryGetProperty("entries", out var entries))
        {
            return flows;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            var flow = HarEntryParser.ParseEntry(entry);

            if (flow is not null)
            {
                flows.Add(flow);
            }
        }

        return flows;
    }
}
