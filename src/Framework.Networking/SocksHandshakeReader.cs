using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helpers for reading SOCKS handshake bytes from a <see cref="PipeReader" />.
/// </summary>
public static class SocksHandshakeReader
{
    /// <summary>
    ///     Detects the SOCKS protocol version from the first byte available on the reader
    ///     without consuming it. Returns null when the reader is empty or the byte does not
    ///     match a SOCKS version. The reader's examined pointer is advanced to the end of the
    ///     buffer so subsequent <see cref="PipeReader.ReadAsync(CancellationToken)" /> calls
    ///     are valid.
    /// </summary>
    /// <param name="reader">The reader to peek.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The detected version, or null.</returns>
    public static async Task<SocksVersion?> DetectVersionAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        var result = await PipeReaderHelper.ReadUntilAsync(reader, 1, cancellationToken).ConfigureAwait(false);

        if (result.Buffer.Length == 0)
        {
            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            return null;
        }

        var version = SocksProtocolDetector.Detect(result.Buffer);
        reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        return version;
    }

    /// <summary>
    ///     Returns true when the SOCKS5 client's offered method list includes the
    ///     No-Authentication method (0x00).
    /// </summary>
    /// <param name="methods">The offered methods.</param>
    /// <returns>True when 0x00 is in the list.</returns>
    public static bool HasNoAuthMethod(System.Collections.Generic.IReadOnlyList<byte> methods)
    {
        foreach (var method in methods)
        {
            if (method == 0x00)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Reads at least <paramref name="minimumBytes" /> from the reader into a byte array
    ///     without consuming any data (the data remains available for subsequent reads),
    ///     copying at most <paramref name="maximumBytes" /> bytes from the front of the
    ///     buffered sequence. The cap prevents a client that pipelines a large payload
    ///     immediately after the SOCKS handshake from forcing an avoidable large allocation
    ///     on the proxy's hot connection path. The reader's examined pointer is advanced to
    ///     the end of the buffer so that calling
    ///     <see cref="PipeReader.ReadAsync(CancellationToken)" /> after this method is valid.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="minimumBytes">Minimum bytes to buffer before returning.</param>
    /// <param name="maximumBytes">
    ///     Maximum number of bytes to copy into the returned array. Must be greater than or
    ///     equal to <paramref name="minimumBytes" />.
    /// </param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>
    ///     The leading bytes of the buffered sequence as an array, with length bounded by
    ///     <paramref name="maximumBytes" />.
    /// </returns>
    public static async Task<byte[]> ReadIntoArrayAsync(PipeReader reader, int minimumBytes, int maximumBytes, CancellationToken cancellationToken)
    {
        var result = await PipeReaderHelper.ReadUntilAsync(reader, minimumBytes, cancellationToken).ConfigureAwait(false);
        var sliceLength = (int)System.Math.Min(result.Buffer.Length, maximumBytes);
        var bytes = result.Buffer.Slice(0, sliceLength).ToArray();
        reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        return bytes;
    }
}
