using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Reassembles a sequence of WebSocket frames into completed messages, joining
///     continuation frames per RFC 6455 Â§ 5.4. Control frames (Ping/Pong/Close) are
///     emitted immediately and never fragmented.
/// </summary>
public sealed class WebSocketMessageAssembler
{
    private readonly List<byte> _pendingPayload;
    private WebSocketOpcode _pendingOpcode;

    /// <summary>
    ///     Gets a value indicating whether the assembler currently has a partially-received
    ///     message awaiting continuation frames.
    /// </summary>
    public bool IsAccumulating { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="WebSocketMessageAssembler" />.
    /// </summary>
    public WebSocketMessageAssembler()
    {
        var pending = new List<byte>();
        _pendingPayload = pending;
        _pendingOpcode = WebSocketOpcode.Continuation;
        IsAccumulating = false;
    }

    /// <summary>
    ///     Accepts the next frame and returns a completed <see cref="WebSocketMessage" /> when
    ///     one is ready, otherwise null (continuation pending).
    /// </summary>
    /// <param name="frame">The parsed frame.</param>
    /// <param name="direction">The message direction.</param>
    /// <param name="timestamp">The timestamp to assign to a completed message.</param>
    /// <returns>The completed message, or null when more continuation frames are required.</returns>
    /// <exception cref="System.IO.InvalidDataException">
    ///     Thrown when a continuation frame is received with no in-progress message, or when a
    ///     new data-message frame arrives while a message is still in progress.
    /// </exception>
    public WebSocketMessage? Accept(WebSocketFrame frame, WebSocketDirection direction, DateTimeOffset timestamp)
    {
        if (WebSocketOpcodes.HasControlBehavior(frame.Opcode))
        {
            var controlMessage = new WebSocketMessage(direction, frame.Opcode, frame.Payload, timestamp);
            return controlMessage;
        }

        if (frame.Opcode == WebSocketOpcode.Continuation)
        {
            if (!IsAccumulating)
            {
                throw new System.IO.InvalidDataException("Continuation frame received with no in-progress message.");
            }

            AppendPayload(frame.Payload.Span);
        }
        else
        {
            if (IsAccumulating)
            {
                throw new System.IO.InvalidDataException("New data message frame received while a previous message is in progress.");
            }

            _pendingOpcode = frame.Opcode;
            _pendingPayload.Clear();
            IsAccumulating = true;
            AppendPayload(frame.Payload.Span);
        }

        if (!frame.IsFinalFragment)
        {
            return null;
        }

        var completed = new WebSocketMessage(direction, _pendingOpcode, _pendingPayload.ToArray(), timestamp);
        _pendingPayload.Clear();
        IsAccumulating = false;
        return completed;
    }

    private void AppendPayload(ReadOnlySpan<byte> payload)
    {
        for (var index = 0; index < payload.Length; index++)
        {
            _pendingPayload.Add(payload[index]);
        }
    }
}
