using System;
using System.Buffers;
using System.Globalization;
using System.Text;

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
        var decodedMessage = DecodeMessage(messageHeaderValue);
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

    /// <summary>
    ///     Decodes a grpc-message trailer value per the gRPC percent-encoding rules.
    ///     Bytes outside the unreserved set (0x20-0x7E except '%') are encoded as %XX
    ///     over the UTF-8 representation. Malformed escapes are passed through verbatim.
    /// </summary>
    private static string? DecodeMessage(string? messageHeaderValue)
    {
        if (string.IsNullOrWhiteSpace(messageHeaderValue))
        {
            return null;
        }

        if (messageHeaderValue.IndexOf('%') < 0)
        {
            return messageHeaderValue;
        }

        var length = messageHeaderValue.Length;
        var byteBuffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            var byteCount = 0;
            var index = 0;
            while (index < length)
            {
                var current = messageHeaderValue[index];
                if (current == '%' && index + 2 < length)
                {
                    var decodedByte = TryDecodeHexByte(messageHeaderValue[index + 1], messageHeaderValue[index + 2]);
                    if (decodedByte is not null)
                    {
                        byteBuffer[byteCount] = decodedByte.Value;
                        byteCount++;
                        index += 3;
                        continue;
                    }
                }

                if (current <= 0x7F)
                {
                    byteBuffer[byteCount] = (byte)current;
                    byteCount++;
                }
                else
                {
                    var charSlice = messageHeaderValue.AsSpan(index, 1);
                    byteCount += Encoding.UTF8.GetBytes(charSlice, byteBuffer.AsSpan(byteCount));
                }

                index++;
            }

            return Encoding.UTF8.GetString(byteBuffer, 0, byteCount);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuffer);
        }
    }

    private static byte? TryDecodeHexByte(char high, char low)
    {
        var highNibble = TryDecodeHexDigit(high);
        var lowNibble = TryDecodeHexDigit(low);
        if (highNibble is null || lowNibble is null)
        {
            return null;
        }

        return (byte)((highNibble.Value << 4) | lowNibble.Value);
    }

    private static int? TryDecodeHexDigit(char character)
    {
        if (character is >= '0' and <= '9')
        {
            return character - '0';
        }

        if (character is >= 'a' and <= 'f')
        {
            return 10 + (character - 'a');
        }

        if (character is >= 'A' and <= 'F')
        {
            return 10 + (character - 'A');
        }

        return null;
    }
}
