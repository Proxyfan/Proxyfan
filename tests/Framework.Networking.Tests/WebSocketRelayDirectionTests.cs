using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="WebSocketRelayDirection" /> verifying that the relay direction
///     pumps bytes from source to destination, swallows expected exceptions, and always
///     signals its paired direction via <see cref="WebSocketRelayDirectionRequest.LinkedSource" />.
/// </summary>
public sealed class WebSocketRelayDirectionTests
{
    /// <summary>Verifies that frames pumped by the underlying relay reach the destination.</summary>
    [Test]
    public async Task RelayAsync_SingleUnmaskedTextFrame_ForwardsToDestination()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, captured.Add, TimeProvider.System);
        var frame = BuildUnmaskedTextFrame("hello");
        using var source = new MemoryStream(frame);
        using var destination = new MemoryStream();
        using var linked = new CancellationTokenSource();
        var request = new WebSocketRelayDirectionRequest
        {
            Destination = destination,
            LinkedSource = linked,
            Relay = relay,
            Source = source,
        };

        await WebSocketRelayDirection.RelayAsync(request, CancellationToken.None);

        await Assert.That(destination.ToArray()).IsEquivalentTo(frame);
        await Assert.That(captured.Count).IsEqualTo(1);
    }

    /// <summary>Verifies that the linked source is cancelled when the relay completes normally.</summary>
    [Test]
    public async Task RelayAsync_SourceCompletes_CancelsLinkedSource()
    {
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, _ => { }, TimeProvider.System);
        using var source = new MemoryStream();
        using var destination = new MemoryStream();
        using var linked = new CancellationTokenSource();
        var request = new WebSocketRelayDirectionRequest
        {
            Destination = destination,
            LinkedSource = linked,
            Relay = relay,
            Source = source,
        };

        await WebSocketRelayDirection.RelayAsync(request, CancellationToken.None);

        await Assert.That(linked.IsCancellationRequested).IsTrue();
    }

    /// <summary>Verifies that cancellation aborts cleanly without throwing out of the method.</summary>
    [Test]
    public async Task RelayAsync_CancellationRequested_SwallowsAndCancelsLinkedSource()
    {
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, _ => { }, TimeProvider.System);
        using var source = new BlockingStream();
        using var destination = new MemoryStream();
        using var linked = new CancellationTokenSource();
        using var trigger = new CancellationTokenSource();
        var request = new WebSocketRelayDirectionRequest
        {
            Destination = destination,
            LinkedSource = linked,
            Relay = relay,
            Source = source,
        };

        var pump = WebSocketRelayDirection.RelayAsync(request, trigger.Token);
        await trigger.CancelAsync();
        await pump;

        await Assert.That(linked.IsCancellationRequested).IsTrue();
    }

    /// <summary>Verifies that I/O exceptions are swallowed and the linked source is cancelled.</summary>
    [Test]
    public async Task RelayAsync_SourceThrowsIoException_SwallowsAndCancelsLinkedSource()
    {
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, _ => { }, TimeProvider.System);
        using var source = new ThrowingStream();
        using var destination = new MemoryStream();
        using var linked = new CancellationTokenSource();
        var request = new WebSocketRelayDirectionRequest
        {
            Destination = destination,
            LinkedSource = linked,
            Relay = relay,
            Source = source,
        };

        await WebSocketRelayDirection.RelayAsync(request, CancellationToken.None);

        await Assert.That(linked.IsCancellationRequested).IsTrue();
    }

    private static byte[] BuildUnmaskedTextFrame(string text)
    {
        var payload = Encoding.UTF8.GetBytes(text);
        var bytes = new byte[2 + payload.Length];
        bytes[0] = 0x81;
        bytes[1] = (byte)payload.Length;
        Array.Copy(payload, 0, bytes, 2, payload.Length);
        return bytes;
    }

    private sealed class BlockingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => 0;

        public override long Position { get; set; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<int>();
            using var registration = cancellationToken.Register(static state => ((TaskCompletionSource<int>)state!).TrySetCanceled(), tcs);
            return await tcs.Task.ConfigureAwait(false);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }

    private sealed class ThrowingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => 0;

        public override long Position { get; set; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException("simulated");
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new IOException("simulated");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}
