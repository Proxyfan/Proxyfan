using Proxyfan.Domain.Session.Har;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Renders a HAR file's contents as a human-readable text summary.
/// </summary>
public sealed class HarSummaryRenderer
{
    private readonly IHarImporter _importer;

    /// <summary>
    ///     Initializes a new <see cref="HarSummaryRenderer" />.
    /// </summary>
    /// <param name="importer">The HAR importer.</param>
    public HarSummaryRenderer(IHarImporter importer)
    {
        _importer = importer;
    }

    /// <summary>
    ///     Reads the HAR document from the supplied input stream and writes a human-readable
    ///     summary to the supplied output text writer.
    /// </summary>
    /// <param name="input">The HAR JSON input stream.</param>
    /// <param name="output">The text writer to receive the summary.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the summary has been written.</returns>
    public async Task RenderAsync(Stream input, TextWriter output, CancellationToken cancellationToken)
    {
        var flows = await _importer.ImportAsync(input, cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync($"Proxyfan HAR Summary — {flows.Count} flow(s)".AsMemory(), cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync(string.Empty.AsMemory(), cancellationToken).ConfigureAwait(false);

        for (var index = 0; index < flows.Count; index++)
        {
            var flow = flows[index];
            var line = HarSummaryFormatter.BuildFlowLine(index + 1, flow);
            await output.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }
}
