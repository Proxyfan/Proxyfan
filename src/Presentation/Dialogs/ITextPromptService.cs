using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Dialogs;

/// <summary>
///     Abstraction over a single-line modal text prompt so view models can request a
///     short string from the user (e.g. a comment to attach to a flow) without coupling
///     to a UI framework.
/// </summary>
public interface ITextPromptService
{
    /// <summary>
    ///     Displays a modal text prompt and returns the value the user entered, or
    ///     <c>null</c> if the user cancelled.
    /// </summary>
    /// <param name="request">Describes the dialog title, label, and pre-filled value.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The accepted value or <c>null</c> when the user cancelled the dialog.</returns>
    Task<string?> PromptAsync(TextPromptRequest request, CancellationToken cancellationToken);
}
