using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Sends a composed <see cref="HypertextTransferProtocolRequestData" /> over the network and
///     returns the captured response. Used by the Request Composer tool to replay or compose
///     ad-hoc requests without going through the proxy listener.
/// </summary>
public interface IComposerRequestSender
{
    /// <summary>
    ///     Sends the composed request and returns the response data.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token to cancel the send.</param>
    /// <returns>
    ///     A successful result containing the response received from the upstream server, or a
    ///     failure result containing a <see cref="ComposerSendError" />.
    /// </returns>
    Task<Result<HypertextTransferProtocolResponseData>> SendAsync(
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken);
}
