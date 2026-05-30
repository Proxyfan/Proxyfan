using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Proxyfan.Client.Clipboard;
using Proxyfan.Client.Dialogs;
using Proxyfan.Client.Files;
using Proxyfan.Presentation;

namespace Proxyfan.Client.Shell.Views;

/// <summary>
///     The main application window hosting the shell content for desktop platforms.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "XAML view code-behind: Avalonia-generated wiring with no testable logic.")]
public partial class ShellWindow : Window
{
    /// <summary>
    ///     Initializes a new instance of <see cref="ShellWindow" />.
    /// </summary>
    public ShellWindow()
    {
        InitializeComponent();
        Opened += OnOpenedRegisterFilePicker;
        KeyDown += OnKeyDownFocusFilter;
    }

    private void OnKeyDownFocusFilter(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.F || !eventArgs.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        var filterTextBox = this.FindDescendantOfType<TextBox>(includeSelf: false);
        TextBox? namedFilterTextBox = null;
        foreach (var control in this.GetVisualDescendants())
        {
            if (control is TextBox textBox && string.Equals(textBox.Name, "FilterTextBox", System.StringComparison.Ordinal))
            {
                namedFilterTextBox = textBox;
                break;
            }
        }

        var target = namedFilterTextBox ?? filterTextBox;
        if (target is null)
        {
            return;
        }

        target.Focus();
        target.SelectAll();
        eventArgs.Handled = true;
    }

    private void OnOpenedRegisterFilePicker(object? sender, System.EventArgs eventArgs)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var services = ContainerLocator.Current;
        if (services is null)
        {
            return;
        }

        var picker = services.GetService<AvaloniaFilePickerService>();
        picker?.RegisterTopLevel(topLevel);

        var clipboard = services.GetService<AvaloniaClipboardService>();
        clipboard?.RegisterTopLevel(topLevel);

        var prompt = services.GetService<AvaloniaTextPromptService>();
        prompt?.RegisterOwner(this);
    }
}