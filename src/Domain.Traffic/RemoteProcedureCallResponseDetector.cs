using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Detects whether an HTTP response header collection indicates a Remote Procedure Call
///     (gRPC) stream. Looks for a <c>Content-Type</c> value whose media type is exactly
///     <c>application/grpc</c> per the gRPC HTTP/2 wire spec — covers <c>application/grpc</c>,
///     <c>application/grpc+proto</c>, <c>application/grpc+json</c>, and other <c>+suffix</c>
///     forms, optionally followed by <c>;</c> parameters. Sibling media types such as
///     <c>application/grpc-web</c> are treated as distinct and are not detected.
/// </summary>
public static class RemoteProcedureCallResponseDetector
{
    private const string ContentTypeHeaderName = "Content-Type";
    private const string ContentTypePrefix = "application/grpc";

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied response headers carry a
    ///     <c>Content-Type</c> value whose media type is <c>application/grpc</c> or
    ///     <c>application/grpc+suffix</c>.
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

        if (!value.StartsWith(ContentTypePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (value.Length == ContentTypePrefix.Length)
        {
            return true;
        }

        var next = value[ContentTypePrefix.Length];
        return next is '+' or ';';
    }
}
