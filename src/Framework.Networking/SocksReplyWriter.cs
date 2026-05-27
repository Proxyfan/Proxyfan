using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helpers for writing SOCKS protocol reply messages to a pipe writer.
/// </summary>
public static class SocksReplyWriter
{
    private static readonly byte[] Socks4FailureReply;
    private static readonly byte[] Socks4SuccessReply;
    private static readonly byte[] Socks5GeneralFailureReply;
    private static readonly byte[] Socks5SuccessReply;

    static SocksReplyWriter()
    {
        Socks4SuccessReply = [0x00, 0x5A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        Socks4FailureReply = [0x00, 0x5B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        Socks5SuccessReply = [0x05, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        Socks5GeneralFailureReply = [0x05, 0x05, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    }

    /// <summary>
    ///     Writes a SOCKS4 reply message and flushes the writer.
    /// </summary>
    /// <param name="output">The pipe writer.</param>
    /// <param name="isSuccess">True for granted (0x5A), false for rejected (0x5B).</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public static async Task WriteSocks4ReplyAsync(PipeWriter output, bool isSuccess, CancellationToken cancellationToken)
    {
        var reply = isSuccess ? Socks4SuccessReply : Socks4FailureReply;
        await output.WriteAsync(reply, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Writes a SOCKS5 general failure reply (REP=0x05) and flushes the writer.
    /// </summary>
    /// <param name="output">The pipe writer.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public static async Task WriteSocks5FailureReplyAsync(PipeWriter output, CancellationToken cancellationToken)
    {
        await output.WriteAsync(Socks5GeneralFailureReply, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Writes a SOCKS5 success reply (REP=0x00) and flushes the writer.
    /// </summary>
    /// <param name="output">The pipe writer.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public static async Task WriteSocks5SuccessReplyAsync(PipeWriter output, CancellationToken cancellationToken)
    {
        await output.WriteAsync(Socks5SuccessReply, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
