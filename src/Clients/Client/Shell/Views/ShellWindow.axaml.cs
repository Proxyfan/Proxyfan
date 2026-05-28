using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
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

        var prompt = services.GetService<AvaloniaTextPromptService>();
        prompt?.RegisterOwner(this);
    }
}