using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Helper that derives the exact total length of a SOCKS5 CONNECT request from its
///     already-buffered 4-byte prefix (VER, CMD, RSV, ATYP). For domain-name targets the
///     helper reads one extra byte to learn the variable-length destination.
/// </summary>
public static class Socks5RequestLengthDeriver
{
    /// <summary>
    ///     Returns the total expected request length in bytes, or <see langword="null" /> when
    ///     the address type is unrecognised or the domain-name length byte cannot be read.
    /// </summary>
    /// <param name="reader">The pipe reader the prefix was read from.</param>
    /// <param name="prefix">The 4-byte SOCKS5 CONNECT request prefix.</param>
    /// <param name="maximumBytes">The cap to apply to the secondary read for the length byte.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total request length to read; <see langword="null" /> when undetermined.</returns>
    public static async Task<int?> DeriveRequiredLengthAsync(PipeReader reader, byte[] prefix, int maximumBytes, CancellationToken cancellationToken)
    {
        var addressType = (Socks5AddressType)prefix[3];
        switch (addressType)
        {
            case Socks5AddressType.InternetProtocolVersionFour:
                return 10;
            case Socks5AddressType.InternetProtocolVersionSix:
                return 22;
            case Socks5AddressType.DomainName:
                var withLengthByte = await SocksHandshakeReader.ReadIntoArrayAsync(reader, 5, maximumBytes, cancellationToken).ConfigureAwait(false);
                if (withLengthByte.Length < 5)
                {
                    return null;
                }

                return 5 + withLengthByte[4] + 2;
            default:
                return null;
        }
    }
}
