using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     <see cref="HttpClient" />-backed implementation of <see cref="IComposerRequestSender" />.
///     Sends composed requests directly (not through the proxy listener) so the Composer tool
///     stays usable even when the local proxy is stopped, mirroring the behaviour of Charles'
///     "Repeat" and Fiddler's Composer tools.
/// </summary>
public sealed class ComposerRequestSender : IComposerRequestSender
{
    private readonly HttpClient _hypertextTransferProtocolClient;

    /// <summary>
    ///     Initializes a new <see cref="ComposerRequestSender" /> using the supplied
    ///     <see cref="HttpClient" />. The caller owns the lifetime of the client.
    /// </summary>
    /// <param name="hypertextTransferProtocolClient">The shared HTTP client to use for outbound requests.</param>
    public ComposerRequestSender(HttpClient hypertextTransferProtocolClient)
    {
        _hypertextTransferProtocolClient = hypertextTransferProtocolClient;
    }

    /// <inheritdoc />
    public async Task<Result<HypertextTransferProtocolResponseData>> SendAsync(
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var message = ComposerRequestMessageBuilder.Build(request);
            using var response = await _hypertextTransferProtocolClient
                .SendAsync(message, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);
            var bodyBytes = await response.Content
                .ReadAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            var headers = ComposerResponseHeaderProjector.Project(response);
            var reasonPhrase = response.ReasonPhrase ?? string.Empty;
            var parameters = new HypertextTransferProtocolResponseDataParameters
            {
                Body = bodyBytes,
                Headers = headers,
                ReasonPhrase = reasonPhrase,
                StatusCode = (int)response.StatusCode,
                Version = "HTTP/" + response.Version,
            };
            var responseData = new HypertextTransferProtocolResponseData(parameters);
            return Result.Success(responseData);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var error = new ComposerSendError(ex.Message, ex);
            return Result.Failure<HypertextTransferProtocolResponseData>(error);
        }
    }
}
