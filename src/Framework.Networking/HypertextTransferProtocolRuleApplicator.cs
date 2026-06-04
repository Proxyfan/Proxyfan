using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helpers for the HTTP proxy handler that apply rule actions, build wire-format
///     exchanges from modified request/response data, and serialize headers.
/// </summary>
public static class HypertextTransferProtocolRuleApplicator
{
    private static readonly byte[] BlockedResponseBytes;

    static HypertextTransferProtocolRuleApplicator()
    {
        var blockedResponse = Encoding.ASCII.GetBytes("HTTP/1.1 403 Forbidden\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        BlockedResponseBytes = blockedResponse;
    }

    /// <summary>
    ///     Walks the supplied request actions and returns the final modified request after
    ///     applying all <c>Redirect</c> and <c>ModifyRequest</c> actions in order.
    /// </summary>
    /// <param name="originalRequest">The request to apply modifications to.</param>
    /// <param name="actions">The actions to apply.</param>
    /// <returns>The final modified request.</returns>
    public static HypertextTransferProtocolRequestData ApplyRequestModifications(
        HypertextTransferProtocolRequestData originalRequest,
        IReadOnlyList<RequestPipelineAction> actions)
    {
        var currentRequest = originalRequest;

        foreach (var action in actions)
        {
            if (action is RequestPipelineAction.Redirect redirect)
            {
                currentRequest = redirect.RewrittenRequest;
            }
            else if (action is RequestPipelineAction.ModifyRequest modify)
            {
                currentRequest = modify.ModifiedRequest;
            }
        }

        return currentRequest;
    }

    /// <summary>
    ///     Walks the supplied response actions and returns the final modified response after
    ///     applying all <c>ModifyResponse</c> actions in order.
    /// </summary>
    /// <param name="originalResponse">The response to apply modifications to.</param>
    /// <param name="actions">The actions to apply.</param>
    /// <returns>The final modified response.</returns>
    public static HypertextTransferProtocolResponseData ApplyResponseModifications(
        HypertextTransferProtocolResponseData originalResponse,
        IReadOnlyList<ResponsePipelineAction> actions)
    {
        var currentResponse = originalResponse;

        foreach (var action in actions)
        {
            if (action is ResponsePipelineAction.ModifyResponse modify)
            {
                currentResponse = modify.ModifiedResponse;
            }
        }

        return currentResponse;
    }

    /// <summary>
    ///     Builds a synthetic exchange that wraps a locally-generated response from a Map Local rule.
    /// </summary>
    /// <param name="localResponse">The local response data.</param>
    /// <returns>A response exchange ready to write to the client.</returns>
    public static HypertextTransferProtocolProxyResponseExchange BuildLocalResponseExchange(
        HypertextTransferProtocolResponseData localResponse)
    {
        var headerText = BuildResponseHeaderText(localResponse);
        var headerBytes = Encoding.ASCII.GetBytes(headerText);
        var exchange = new HypertextTransferProtocolProxyResponseExchange(
            localResponse.Body,
            headerBytes,
            localResponse);
        return exchange;
    }

    /// <summary>
    ///     Returns the original exchange unchanged when the effective request is the same instance;
    ///     otherwise rebuilds the wire-format header bytes from the modified request.
    /// </summary>
    /// <param name="originalExchange">The original request exchange.</param>
    /// <param name="effectiveRequest">The request after rule modifications.</param>
    /// <returns>An exchange whose header bytes match the effective request.</returns>
    public static HypertextTransferProtocolProxyRequestExchange BuildRequestExchangeWith(
        HypertextTransferProtocolProxyRequestExchange originalExchange,
        HypertextTransferProtocolRequestData effectiveRequest)
    {
        if (ReferenceEquals(originalExchange.Request, effectiveRequest))
        {
            return originalExchange;
        }

        var headerText = BuildRequestHeaderText(effectiveRequest);
        var headerBytes = Encoding.ASCII.GetBytes(headerText);
        var newExchange = new HypertextTransferProtocolProxyRequestExchange(
            effectiveRequest.Body,
            headerBytes,
            effectiveRequest);
        return newExchange;
    }

    /// <summary>
    ///     Returns the original exchange unchanged when the final response is the same instance;
    ///     otherwise rebuilds the wire-format header bytes from the modified response.
    /// </summary>
    /// <param name="originalExchange">The original response exchange.</param>
    /// <param name="finalResponse">The response after rule modifications.</param>
    /// <returns>An exchange whose header bytes match the final response.</returns>
    public static HypertextTransferProtocolProxyResponseExchange BuildResponseExchangeWith(
        HypertextTransferProtocolProxyResponseExchange originalExchange,
        HypertextTransferProtocolResponseData finalResponse)
    {
        if (ReferenceEquals(originalExchange.Response, finalResponse))
        {
            return originalExchange;
        }

        var headerText = BuildResponseHeaderText(finalResponse);
        var headerBytes = Encoding.ASCII.GetBytes(headerText);
        var newExchange = new HypertextTransferProtocolProxyResponseExchange(
            finalResponse.Body,
            headerBytes,
            finalResponse);
        return newExchange;
    }

    /// <summary>
    ///     Builds a synthetic blocked-response data object suitable for storing in the traffic flow
    ///     when a Block rule short-circuits the request.
    /// </summary>
    /// <returns>A 403 Forbidden response data object.</returns>
    public static HypertextTransferProtocolResponseData CreateBlockedResponseData()
    {
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = System.Array.Empty<byte>(),
            Headers = HeaderCollection.Empty
                .Add("Content-Length", "0")
                .Add("Connection", "close"),
            ReasonPhrase = "Forbidden",
            StatusCode = 403,
            Version = "HTTP/1.1",
        };
        var responseData = new HypertextTransferProtocolResponseData(responseParameters);
        return responseData;
    }

    /// <summary>
    ///     Finds the first short-circuiting action (Block, Pause, or ServeLocalResponse) in the
    ///     supplied action list, or returns <see langword="null" /> when no such action is present.
    /// </summary>
    /// <param name="actions">The actions returned by the rule engine.</param>
    /// <returns>The first blocking, pause, or local-response action, or null.</returns>
    public static RequestPipelineAction? FindBlockingAction(IReadOnlyList<RequestPipelineAction> actions)
    {
        foreach (var action in actions)
        {
            if (action is RequestPipelineAction.Block or RequestPipelineAction.Pause or RequestPipelineAction.ServeLocalResponse)
            {
                return action;
            }
        }

        return null;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied response actions contain a
    ///     <see cref="ResponsePipelineAction.Pause" /> action indicating the breakpoint rule
    ///     aborted the response phase.
    /// </summary>
    /// <param name="actions">The actions returned by the rule engine.</param>
    /// <returns><see langword="true" /> when a pause action is present; otherwise <see langword="false" />.</returns>
    public static bool HasResponsePauseAction(IReadOnlyList<ResponsePipelineAction> actions)
    {
        foreach (var action in actions)
        {
            if (action is ResponsePipelineAction.Pause)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Writes a canonical 403 Forbidden response to the supplied client output pipe.
    /// </summary>
    /// <param name="output">The pipe writer to send bytes to.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the response is flushed.</returns>
    public static async Task SendBlockedResponseAsync(PipeWriter output, CancellationToken cancellationToken)
    {
        await output.WriteAsync(BlockedResponseBytes, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string BuildRequestHeaderText(HypertextTransferProtocolRequestData request)
    {
        var builder = new StringBuilder();
        builder.Append(request.Method).Append(' ').Append(request.RequestUri).Append(' ').Append(request.Version).Append("\r\n");

        foreach (var header in request.Headers)
        {
            foreach (var value in header.Value)
            {
                builder.Append(header.Key).Append(": ").Append(value).Append("\r\n");
            }
        }

        builder.Append("\r\n");
        return builder.ToString();
    }

    private static string BuildResponseHeaderText(HypertextTransferProtocolResponseData response)
    {
        var builder = new StringBuilder();
        builder.Append(response.Version).Append(' ').Append(response.StatusCode).Append(' ').Append(response.ReasonPhrase).Append("\r\n");

        foreach (var header in response.Headers)
        {
            foreach (var value in header.Value)
            {
                builder.Append(header.Key).Append(": ").Append(value).Append("\r\n");
            }
        }

        builder.Append("\r\n");
        return builder.ToString();
    }
}
