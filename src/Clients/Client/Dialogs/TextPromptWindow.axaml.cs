using Avalonia.Controls;
using Avalonia.Interactivity;
using Proxyfan.Presentation.Dialogs;

namespace Proxyfan.Client.Dialogs;

/// <summary>
///     Simple modal window that prompts the user for a single line of text and
///     returns the entered value via <see cref="Window.ShowDialog{TResult}" />.
///     Returns <c>null</c> when the user cancels.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "XAML view code-behind: Avalonia-generated wiring with no testable logic.")]
public partial class TextPromptWindow : Window
{
    /// <summary>
    ///     Initializes a new <see cref="TextPromptWindow" />.
    /// </summary>
    public TextPromptWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Configures the window with the supplied request: applies the title, label,
    ///     and pre-fills the input.
    /// </summary>
    /// <param name="request">The prompt configuration.</param>
    public void Configure(TextPromptRequest request)
    {
        Title = request.Title;
        LabelText.Text = request.Label;
        ValueInput.Text = request.InitialValue ?? string.Empty;
        ValueInput.CaretIndex = ValueInput.Text.Length;
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs routedEventArgs)
    {
        Close(null);
    }

    private void OnOkClicked(object? sender, RoutedEventArgs routedEventArgs)
    {
        Close(ValueInput.Text ?? string.Empty);
    }
}
