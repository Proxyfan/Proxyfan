using System;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="UpgradePrefixedStreamFactory" />.
/// </summary>
public sealed class UpgradePrefixedStreamFactoryTests
{
    /// <summary>
    ///     Verifies that <see cref="UpgradePrefixedStreamFactory.WrapWithPrefix" /> returns the
    ///     inner stream unchanged when the prefix is empty.
    /// </summary>
    [Test]
    public async Task WrapWithPrefix_EmptyPrefix_ReturnsInnerStream()
    {
        using var inner = new MemoryStream();

        var result = UpgradePrefixedStreamFactory.WrapWithPrefix(Array.Empty<byte>(), inner);

        await Assert.That(result).IsSameReferenceAs(inner);
    }

    /// <summary>
    ///     Verifies that <see cref="UpgradePrefixedStreamFactory.WrapWithPrefix" /> returns a
    ///     <see cref="PrefixedReadStream" /> when the prefix has bytes, and the prefix is
    ///     replayed before the inner stream's data.
    /// </summary>
    [Test]
    public async Task WrapWithPrefix_NonEmptyPrefix_ReplaysPrefixThenInner()
    {
        var prefix = new byte[] { 1, 2 };
        using var inner = new MemoryStream(new byte[] { 3, 4 });

        var wrapped = UpgradePrefixedStreamFactory.WrapWithPrefix(prefix, inner);

        await Assert.That(wrapped).IsTypeOf<PrefixedReadStream>();
        var buffer = new byte[4];
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await wrapped.ReadAsync(buffer.AsMemory(total));
            if (read == 0)
            {
                break;
            }
            total += read;
        }
        await Assert.That(total).IsEqualTo(4);
        await Assert.That(buffer).IsEquivalentTo(new byte[] { 1, 2, 3, 4 });
    }
}
