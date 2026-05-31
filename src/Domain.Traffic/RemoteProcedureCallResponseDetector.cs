using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Detects whether an HTTP response header collection indicates a Remote Procedure Call
///     (gRPC) stream. Looks for a <c>Content-Type</c> value starting with
///     <c>application/grpc</c> per the gRPC HTTP/2 wire spec — covers <c>application/grpc</c>,
///     <c>application/grpc+proto</c>, <c>application/grpc+json</c>, and other suffixes.
/// </summary>
public static class RemoteProcedureCallResponseDetector
{
    private const string ContentTypeHeaderName = "Content-Type";
    private const string ContentTypePrefix = "application/grpc";

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied response headers carry a
    ///     <c>Content-Type</c> value starting with <c>application/grpc</c>.
    /// </summary>
    /// <param name="headers">The response headers to inspect.</param>
    /// <returns><see langword="true" /> when the response is a gRPC stream.</returns>
    public static bool HasRemoteProcedureCallResponse(HeaderCollection headers)
    {
        var value = headers.Get(ContentTypeHeaderName);
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return value.StartsWith(ContentTypePrefix, StringComparison.OrdinalIgnoreCase);
    }
}
