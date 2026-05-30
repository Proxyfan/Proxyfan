using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Clipboard;

/// <summary>
///     Abstraction over the platform clipboard so view-model commands can copy text without
///     coupling to a UI framework. Calls are idempotent and silent — failures are surfaced as
///     <see langword="false" /> rather than thrown exceptions so context-menu commands never
///     raise an unhandled exception in the user's UI.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    ///     Sets the supplied <paramref name="text" /> on the system clipboard.
    /// </summary>
    /// <param name="text">The text to copy. <see langword="null" /> or empty strings are treated as a no-op.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    ///     <see langword="true" /> when the text was successfully written to the clipboard;
    ///     <see langword="false" /> when no clipboard provider is available (e.g. running
    ///     headless or before the main window is shown), the text is null/empty, or the
    ///     underlying platform call failed.
    /// </returns>
    Task<bool> SetTextAsync(string? text, CancellationToken cancellationToken);
}
