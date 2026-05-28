using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Unit tests for <see cref="PrefixedReadStream" />. Verifies that the buffered prefix is
///     delivered ahead of the underlying stream and that writes, flushes, and disposal are
///     forwarded to the inner stream.
/// </summary>
public sealed class PrefixedReadStreamTests
{
    /// <summary>Reading delivers prefix bytes first, then continues from the inner stream.</summary>
    [Test]
    public async Task ReadAsync_PrefixThenInner_DeliversPrefixFirst()
    {
        var prefix = Encoding.ASCII.GetBytes("PREFIX-");
        var inner = new MemoryStream(Encoding.ASCII.GetBytes("INNER"));
        var stream = new PrefixedReadStream(prefix, inner);

        var buffer = new byte[64];
        var firstRead = await stream.ReadAsync(buffer, CancellationToken.None);
        await Assert.That(firstRead).IsEqualTo(prefix.Length);
        await Assert.That(Encoding.ASCII.GetString(buffer, 0, firstRead)).IsEqualTo("PREFIX-");

        var secondRead = await stream.ReadAsync(buffer.AsMemory(firstRead), CancellationToken.None);
        await Assert.That(secondRead).IsEqualTo(5);
        await Assert.That(Encoding.ASCII.GetString(buffer, 0, firstRead + secondRead)).IsEqualTo("PREFIX-INNER");
    }

    /// <summary>Synchronous Read returns prefix bytes first then inner bytes.</summary>
    [Test]
    public async Task Read_PrefixThenInner_DeliversPrefixFirst()
    {
        var prefix = Encoding.ASCII.GetBytes("AB");
        var inner = new MemoryStream(Encoding.ASCII.GetBytes("CD"));
        var stream = new PrefixedReadStream(prefix, inner);

        var buffer = new byte[4];
        var first = stream.Read(buffer, 0, buffer.Length);
        await Assert.That(first).IsEqualTo(2);
        var second = stream.Read(buffer, 2, buffer.Length - 2);
        await Assert.That(second).IsEqualTo(2);
        await Assert.That(Encoding.ASCII.GetString(buffer, 0, 4)).IsEqualTo("ABCD");
    }

    /// <summary>An empty prefix delegates straight to the inner stream.</summary>
    [Test]
    public async Task ReadAsync_EmptyPrefix_ReadsInnerImmediately()
    {
        var inner = new MemoryStream(Encoding.ASCII.GetBytes("ONLY-INNER"));
        var stream = new PrefixedReadStream([], inner);

        var buffer = new byte[64];
        var read = await stream.ReadAsync(buffer, CancellationToken.None);
        await Assert.That(Encoding.ASCII.GetString(buffer, 0, read)).IsEqualTo("ONLY-INNER");
    }

    /// <summary>A small buffer returns only what fits and the remainder on the next call.</summary>
    [Test]
    public async Task ReadAsync_SmallBuffer_ReturnsAvailablePrefixOnly()
    {
        var prefix = Encoding.ASCII.GetBytes("0123456789");
        var inner = new MemoryStream();
        var stream = new PrefixedReadStream(prefix, inner);

        var buffer = new byte[4];
        var first = await stream.ReadAsync(buffer, CancellationToken.None);
        await Assert.That(first).IsEqualTo(4);
        await Assert.That(Encoding.ASCII.GetString(buffer, 0, first)).IsEqualTo("0123");

        var second = await stream.ReadAsync(buffer, CancellationToken.None);
        await Assert.That(second).IsEqualTo(4);
        await Assert.That(Encoding.ASCII.GetString(buffer, 0, second)).IsEqualTo("4567");

        var third = await stream.ReadAsync(buffer, CancellationToken.None);
        await Assert.That(third).IsEqualTo(2);
        await Assert.That(Encoding.ASCII.GetString(buffer, 0, third)).IsEqualTo("89");
    }

    /// <summary>Writes are forwarded to the inner stream.</summary>
    [Test]
    public async Task WriteAsync_ForwardsToInner_PreservesBytes()
    {
        var inner = new MemoryStream();
        var stream = new PrefixedReadStream(Array.Empty<byte>(), inner);

        await stream.WriteAsync(Encoding.ASCII.GetBytes("HELLO"), CancellationToken.None);
        await stream.FlushAsync(CancellationToken.None);

        await Assert.That(Encoding.ASCII.GetString(inner.ToArray())).IsEqualTo("HELLO");
    }

    /// <summary>Synchronous Write and Flush forward to the inner stream.</summary>
    [Test]
    public async Task Write_ForwardsToInner_PreservesBytes()
    {
        var inner = new MemoryStream();
        var stream = new PrefixedReadStream([], inner);

        var bytes = Encoding.ASCII.GetBytes("SYNC");
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();

        await Assert.That(Encoding.ASCII.GetString(inner.ToArray())).IsEqualTo("SYNC");
    }

    /// <summary>CanRead and CanWrite reflect the inner stream's capabilities.</summary>
    [Test]
    public async Task Capabilities_DefaultInner_ReflectInnerStream()
    {
        var inner = new MemoryStream();
        var stream = new PrefixedReadStream([], inner);

        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(stream.CanWrite).IsTrue();
        await Assert.That(stream.CanSeek).IsFalse();
    }

    /// <summary>Length, Position, Seek, and SetLength throw NotSupportedException.</summary>
    [Test]
    public async Task UnsupportedOperations_Invoked_ThrowNotSupportedException()
    {
        var stream = new PrefixedReadStream([], new MemoryStream());

        await Assert.That(() => _ = stream.Length).Throws<NotSupportedException>();
        await Assert.That(() => _ = stream.Position).Throws<NotSupportedException>();
        await Assert.That(() => stream.Position = 0).Throws<NotSupportedException>();
        await Assert.That(() => stream.Seek(0, SeekOrigin.Begin)).Throws<NotSupportedException>();
        await Assert.That(() => stream.SetLength(0)).Throws<NotSupportedException>();
    }

    /// <summary>Disposing the stream disposes the inner stream as well.</summary>
    [Test]
    public async Task Dispose_DisposesWrapper_DisposesInnerStream()
    {
        var inner = new MemoryStream();
        var stream = new PrefixedReadStream([], inner);
        stream.Dispose();

        await Assert.That(() => inner.WriteByte(0x00)).Throws<ObjectDisposedException>();
    }
}
