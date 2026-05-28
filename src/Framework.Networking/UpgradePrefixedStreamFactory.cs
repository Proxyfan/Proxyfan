using System.IO;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Helpers for wrapping post-handshake streams with any bytes that were prefetched by
///     the headers reader before the protocol transitioned to a raw byte tunnel.
/// </summary>
public static class UpgradePrefixedStreamFactory
{
    /// <summary>
    ///     Returns <paramref name="inner" /> wrapped in a <see cref="PrefixedReadStream" />
    ///     when <paramref name="prefix" /> contains bytes that must be replayed before any
    ///     fresh read; otherwise returns <paramref name="inner" /> unchanged.
    /// </summary>
    /// <param name="prefix">The bytes already drained from the upstream/client reader.</param>
    /// <param name="inner">The underlying stream that should be read from after the prefix.</param>
    /// <returns>A stream that yields <paramref name="prefix" /> first, then <paramref name="inner" />.</returns>
    public static Stream WrapWithPrefix(byte[] prefix, Stream inner)
    {
        if (prefix.Length == 0)
        {
            return inner;
        }

        var wrapped = new PrefixedReadStream(prefix, inner);
        return wrapped;
    }
}
