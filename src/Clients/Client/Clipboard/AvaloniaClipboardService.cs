using Avalonia.Controls;
using Proxyfan.Presentation.Clipboard;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Clipboard;

/// <summary>
///     Avalonia implementation of <see cref="IClipboardService" /> backed by the top-level
///     window's <c>Clipboard</c>. The active top-level (window) must be registered via
///     <see cref="RegisterTopLevel" /> before the clipboard can be written. All platform
///     exceptions are swallowed so context-menu commands never raise to the user.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Avalonia/host plumbing: requires UI thread/desktop integration, not unit-testable.")]
public sealed class AvaloniaClipboardService : IClipboardService
{
    private TopLevel? _topLevel;

    /// <inheritdoc />
    public async Task<bool> SetTextAsync(string? text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var topLevel = _topLevel;
        var clipboard = topLevel?.Clipboard;
        if (clipboard is null)
        {
            return false;
        }

        try
        {
            await clipboard.SetTextAsync(text).ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    ///     Registers the top-level window whose clipboard the service will write to.
    /// </summary>
    /// <param name="topLevel">The window to register.</param>
    public void RegisterTopLevel(TopLevel? topLevel)
    {
        _topLevel = topLevel;
    }
}
