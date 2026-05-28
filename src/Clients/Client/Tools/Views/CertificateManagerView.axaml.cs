using Avalonia.Controls;
using Avalonia.Interactivity;
using Proxyfan.Client.Tools.ViewModels;

namespace Proxyfan.Client.Tools.Views;

/// <summary>
///     User control for the Certificate Manager tool window. Shows root CA details and
///     hosts the install/uninstall/regenerate/export action buttons.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "XAML view code-behind: Avalonia-generated wiring with no testable logic.")]
public partial class CertificateManagerView : UserControl
{
    /// <summary>
    ///     Initializes a new <see cref="CertificateManagerView" />.
    /// </summary>
    public CertificateManagerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }
    private void OnLoaded(object? sender, RoutedEventArgs args)
    {
        if (DataContext is CertificateManagerViewModel viewModel && viewModel.RefreshCommand.CanExecute(null))
        {
            viewModel.RefreshCommand.Execute(null);
        }
    }
}
