using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    private readonly bool _isGzipCompressed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HarExporter" /> class with no compression.
    /// </summary>
    public HarExporter()
        : this(compressWithGzip: false)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HarExporter" /> class.
    /// </summary>
    /// <param name="compressWithGzip">
    ///     When <c>true</c> the HAR document is written as a gzip-compressed stream,
    ///     producing output suitable for a <c>.har.gz</c> file.
    /// </param>
    public HarExporter(bool compressWithGzip)
    {
        _isGzipCompressed = compressWithGzip;
    }

    /// <inheritdoc />
    public async Task ExportAsync(IReadOnlyList<TrafficFlow> flows, Stream output, CancellationToken cancellationToken)
    {
        if (_isGzipCompressed)
        {
            await using var gzipStream = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true);
            await WriteJsonAsync(gzipStream, flows, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await WriteJsonAsync(output, flows, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteJsonAsync(Stream stream, IReadOnlyList<TrafficFlow> flows, CancellationToken cancellationToken)
    {
        var writerOptions = new JsonWriterOptions
        {
            Indented = true,
        };
        await using var writer = new Utf8JsonWriter(stream, writerOptions);
        HarDocumentWriter.WriteLog(writer, flows, cancellationToken);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
