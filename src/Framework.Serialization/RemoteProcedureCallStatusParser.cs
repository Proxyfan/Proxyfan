using System;
using System.Globalization;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Static parser for gRPC status trailers (grpc-status, grpc-message).
/// </summary>
public static class RemoteProcedureCallStatusParser
{
    /// <summary>
    ///     Parses the supplied trailer values. Returns null when the status header is missing
    ///     or not a numeric integer.
    /// </summary>
    /// <param name="statusHeaderValue">The grpc-status trailer value.</param>
    /// <param name="messageHeaderValue">The grpc-message trailer value, or null.</param>
    /// <returns>The parsed status, or null.</returns>
    public static RemoteProcedureCallStatus? Parse(string? statusHeaderValue, string? messageHeaderValue)
    {
        if (string.IsNullOrWhiteSpace(statusHeaderValue))
        {
            return null;
        }

        if (!int.TryParse(statusHeaderValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawCode))
        {
            return null;
        }

        var typedCode = ConvertRawCode(rawCode);
        var decodedMessage = string.IsNullOrWhiteSpace(messageHeaderValue) ? null : DecodeRemoteProcedureCallMessage(messageHeaderValue);
        var status = new RemoteProcedureCallStatus(rawCode, typedCode, decodedMessage);
        return status;
    }

    private static RemoteProcedureCallStatusCode ConvertRawCode(int rawCode)
    {
        if (rawCode is >= 0 and <= 16)
        {
            return (RemoteProcedureCallStatusCode)rawCode;
        }

        return RemoteProcedureCallStatusCode.Unknown;
    }

    private static string DecodeRemoteProcedureCallMessage(string messageHeaderValue)
    {
        try
        {
            return Uri.UnescapeDataString(messageHeaderValue);
        }
        catch (UriFormatException)
        {
            return messageHeaderValue;
        }
    }
}
