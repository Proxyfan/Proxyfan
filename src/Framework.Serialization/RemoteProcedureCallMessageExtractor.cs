using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Extracts gRPC-prefixed messages from a contiguous byte buffer. The extractor is
///     stateless and pure — callers maintain their own buffering when partial frames span
///     multiple chunks.
/// </summary>
public static class RemoteProcedureCallMessageExtractor
{
    /// <summary>
    ///     Extracts as many complete gRPC messages as the supplied buffer contains. Returns
    ///     both the messages and the number of bytes consumed (so the caller can advance its
    ///     buffer past the consumed prefix).
    /// </summary>
    /// <param name="buffer">The source buffer.</param>
    /// <returns>An extraction result with messages and bytes consumed.</returns>
    /// <exception cref="System.IO.InvalidDataException">
    ///     Thrown when the buffer declares a length that overflows <see cref="int.MaxValue" />.
    /// </exception>
    public static RemoteProcedureCallExtractionResult ExtractAvailable(ReadOnlyMemory<byte> buffer)
    {
        var messages = new List<RemoteProcedureCallMessage>();
        var span = buffer.Span;
        var offset = 0;

        while (offset + 5 <= span.Length)
        {
            var compressionFlag = span[offset];
            var lengthBytes = span.Slice(offset + 1, 4);
            var declaredLength = BinaryPrimitives.ReadUInt32BigEndian(lengthBytes);

            if (declaredLength > int.MaxValue)
            {
                throw new System.IO.InvalidDataException("gRPC frame declares a payload exceeding int.MaxValue bytes.");
            }

            var frameLength = 5 + (int)declaredLength;

            if (offset + frameLength > span.Length)
            {
                break;
            }

            var payloadBytes = span.Slice(offset + 5, (int)declaredLength).ToArray();
            var message = new RemoteProcedureCallMessage(compressionFlag != 0, payloadBytes);
            messages.Add(message);
            offset += frameLength;
        }

        var result = new RemoteProcedureCallExtractionResult(messages, offset);
        return result;
    }
}
