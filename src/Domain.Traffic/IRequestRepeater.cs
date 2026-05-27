using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Replays a previously captured HTTP request through the proxy pipeline so that
///     rule actions (Block / MapLocal / MapRemote / ModifyRequest / ModifyResponse)
///     fire and the new exchange is captured as a fresh <see cref="TrafficFlow" /> with
///     <see cref="TrafficFlowOrigin.Repeated" />. Mirrors Charles' "Repeat" and
///     Fiddler's "Replay" actions.
/// </summary>
public interface IRequestRepeater
{
    /// <summary>
    ///     Repeats the supplied request exactly once.
    /// </summary>
    /// <param name="originalRequest">The request to replay.</param>
    /// <param name="cancellationToken">A token to cancel the replay.</param>
    /// <returns>The flow identifier of the newly captured replay.</returns>
    Task<Guid> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Repeats the supplied request a fixed number of times, optionally with a
    ///     fixed delay between attempts. Operates sequentially - parallel replay is
    ///     reserved for the future load-test surface.
    /// </summary>
    /// <param name="originalRequest">The request to replay.</param>
    /// <param name="repeatCount">How many copies to send. Must be at least one.</param>
    /// <param name="delayBetweenRepeats">Delay between attempts. <see cref="TimeSpan.Zero" /> sends back-to-back.</param>
    /// <param name="cancellationToken">A token to cancel the replay.</param>
    /// <returns>The number of replays that completed before cancellation or failure.</returns>
    Task<int> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        int repeatCount,
        TimeSpan delayBetweenRepeats,
        CancellationToken cancellationToken);
}
