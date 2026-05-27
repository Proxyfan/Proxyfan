using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Result returned by <see cref="RemoteProcedureCallMessageExtractor.ExtractAvailable" />
///     carrying the fully-decoded gRPC messages and the number of bytes consumed from the
///     source buffer.
/// </summary>
public sealed class RemoteProcedureCallExtractionResult
{
    /// <summary>
    ///     Gets the number of bytes the extractor consumed from the source buffer. Callers
    ///     should advance their read cursor by this amount.
    /// </summary>
    public int BytesConsumed { get; }

    /// <summary>
    ///     Gets the fully-decoded gRPC messages produced by this extraction call.
    /// </summary>
    public IReadOnlyList<RemoteProcedureCallMessage> Messages { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallExtractionResult" />.
    /// </summary>
    /// <param name="messages">The extracted messages.</param>
    /// <param name="bytesConsumed">The bytes consumed from the source buffer.</param>
    public RemoteProcedureCallExtractionResult(IReadOnlyList<RemoteProcedureCallMessage> messages, int bytesConsumed)
    {
        Messages = messages;
        BytesConsumed = bytesConsumed;
    }
}
