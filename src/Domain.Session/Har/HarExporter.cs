using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Har;

/// <summary>
///     Default <see cref="IHarExporter" /> implementation that delegates to
///     <see cref="HarDocumentWriter" /> for stream-based HAR 1.2 serialization.
/// </summary>
public sealed class HarExporter : IHarExporter
{
    /// <inheritdoc />
    public async Task ExportAsync(IReadOnlyList<TrafficFlow> flows, Stream output, CancellationToken cancellationToken)
    {
        var writerOptions = new JsonWriterOptions
        {
            Indented = true,
        };
        await using var writer = new Utf8JsonWriter(output, writerOptions);
        HarDocumentWriter.WriteLog(writer, flows, cancellationToken);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
