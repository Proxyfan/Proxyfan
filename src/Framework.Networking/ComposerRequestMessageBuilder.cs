using Proxyfan.Domain.Traffic;
using System.Net.Http;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Builds an <see cref="HttpRequestMessage" /> from a Proxyfan
///     <see cref="HypertextTransferProtocolRequestData" /> for the Composer's
///     <see cref="ComposerRequestSender" />. Headers that belong on the content (e.g.
///     <c>Content-Type</c>) are added to the content rather than the message headers so
///     <see cref="HttpClient" /> doesn't reject them.
/// </summary>
public static class ComposerRequestMessageBuilder
{
    /// <summary>
    ///     Builds an <see cref="HttpRequestMessage" /> from the supplied request data. The
    ///     caller is responsible for disposing the returned message.
    /// </summary>
    /// <param name="request">The request data to project.</param>
    /// <returns>An <see cref="HttpRequestMessage" /> ready to send.</returns>
    public static HttpRequestMessage Build(HypertextTransferProtocolRequestData request)
    {
        var method = new HttpMethod(request.Method);
        var message = new HttpRequestMessage(method, request.RequestUri);

        if (request.Body.Length > 0)
        {
            var content = new ByteArrayContent(request.Body.ToArray());
            message.Content = content;
        }

        foreach (var header in request.Headers)
        {
            var values = new string[header.Value.Length];

            for (var index = 0; index < header.Value.Length; index++)
            {
                values[index] = header.Value[index];
            }

            if (!message.Headers.TryAddWithoutValidation(header.Key, values))
            {
                message.Content?.Headers.TryAddWithoutValidation(header.Key, values);
            }
        }

        return message;
    }
}
