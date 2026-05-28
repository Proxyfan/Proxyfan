using Avalonia.Controls;
using Proxyfan.Presentation.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Dialogs;

/// <summary>
///     Avalonia implementation of <see cref="ITextPromptService" /> that opens
///     <see cref="TextPromptWindow" /> as a modal dialog over the registered
///     top-level window.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Avalonia/host plumbing: requires UI thread/desktop integration, not unit-testable.")]
public sealed class AvaloniaTextPromptService : ITextPromptService
{
    private Window? _ownerWindow;

    /// <inheritdoc />
    public async Task<string?> PromptAsync(TextPromptRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var owner = _ownerWindow;
        if (owner is null)
        {
            return null;
        }

        var window = new TextPromptWindow();
        window.Configure(request);
        var result = await window.ShowDialog<string?>(owner).ConfigureAwait(true);
        return result;
    }

    /// <summary>
    ///     Registers the owner window used to host modal prompts. Call once from the
    ///     shell window's <c>OnAttachedToVisualTree</c> handler.
    /// </summary>
    /// <param name="ownerWindow">The shell window to use as the modal parent.</param>
    public void RegisterOwner(Window ownerWindow)
    {
        _ownerWindow = ownerWindow;
    }
}
