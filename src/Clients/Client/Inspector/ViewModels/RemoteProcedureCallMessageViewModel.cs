using Proxyfan.Domain.Traffic;
using System.Globalization;

namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     View model wrapping a single captured <see cref="RemoteProcedureCallCapturedMessage" />
///     for display in the gRPC inspector message list.
/// </summary>
public sealed class RemoteProcedureCallMessageViewModel
{
    /// <summary>
    ///     Gets the captured message instance.
    /// </summary>
    public RemoteProcedureCallCapturedMessage CapturedMessage { get; }

    /// <summary>
    ///     Gets the displayable compression flag text (<c>"yes"</c> or <c>""</c>).
    /// </summary>
    public string CompressionText { get; }

    /// <summary>
    ///     Gets the short Unicode arrow representing the message direction
    ///     (<c>"↑"</c> for outbound, <c>"↓"</c> for inbound).
    /// </summary>
    public string DirectionGlyph { get; }

    /// <summary>
    ///     Gets a single-line hex preview of the payload bytes (suitable for the row column).
    /// </summary>
    public string PayloadPreview { get; }

    /// <summary>
    ///     Gets the payload size in bytes as a formatted string (e.g. "12 B", "1.5 KB").
    /// </summary>
    public string SizeText { get; }

    /// <summary>
    ///     Gets the wall-clock capture timestamp formatted as <c>HH:mm:ss.fff</c>.
    /// </summary>
    public string TimestampText { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallMessageViewModel" />.
    /// </summary>
    /// <param name="capturedMessage">The captured gRPC message.</param>
    public RemoteProcedureCallMessageViewModel(RemoteProcedureCallCapturedMessage capturedMessage)
    {
        CapturedMessage = capturedMessage;
        DirectionGlyph = capturedMessage.Direction == RemoteProcedureCallDirection.Outbound ? "↑" : "↓";
        PayloadPreview = RemoteProcedureCallPayloadFormatter.FormatPreview(capturedMessage);
        CompressionText = capturedMessage.IsCompressed ? "yes" : string.Empty;
        SizeText = WebSocketByteSizeFormatter.Format(capturedMessage.Payload.Length);
        TimestampText = capturedMessage.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}
