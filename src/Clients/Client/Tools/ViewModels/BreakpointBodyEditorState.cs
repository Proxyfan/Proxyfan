using System;
using System.Text;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Breakpoint body editor state describing how the raw body bytes are surfaced for editing
///     and how they should be re-encoded when the user resumes the paused message.
/// </summary>
public sealed class BreakpointBodyEditorState
{
    private readonly Encoding _encoding;

    /// <summary>
    ///     Gets a value indicating whether the editor text is a base64 representation of the
    ///     original body bytes.
    /// </summary>
    public bool IsBase64 { get; }

    /// <summary>
    ///     Gets the initial editor text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Initializes a new <see cref="BreakpointBodyEditorState" />.
    /// </summary>
    /// <param name="text">The initial editor text.</param>
    /// <param name="isBase64">Whether the editor text is base64.</param>
    /// <param name="encoding">The encoding used for textual bodies.</param>
    public BreakpointBodyEditorState(string text, bool isBase64, Encoding encoding)
    {
        Text = text;
        IsBase64 = isBase64;
        _encoding = encoding;
    }

    /// <summary>
    ///     Encodes the supplied editor text back into bytes. When the text is unchanged, the
    ///     original byte buffer is preserved verbatim.
    /// </summary>
    /// <param name="text">The current editor text.</param>
    /// <param name="originalBody">The original body bytes.</param>
    /// <returns>The body bytes to forward when resuming the breakpoint.</returns>
    public byte[] Encode(string text, ReadOnlyMemory<byte> originalBody)
    {
        text ??= string.Empty;

        if (string.Equals(text, Text, StringComparison.Ordinal))
        {
            return originalBody.ToArray();
        }

        if (text.Length == 0)
        {
            return [];
        }

        if (IsBase64)
        {
            return Convert.FromBase64String(text);
        }

        return _encoding.GetBytes(text);
    }
}
