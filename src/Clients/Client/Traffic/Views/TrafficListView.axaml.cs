using Avalonia.Controls;
using Avalonia.Interactivity;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Presentation.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Traffic.Views;

/// <summary>
///     Code-behind for the traffic list view.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "XAML view code-behind: Avalonia-generated wiring with no testable logic.")]
public partial class TrafficListView : UserControl
{
    private CustomColumnGridSynchronizer? _customColumnSynchronizer;

    /// <summary>
    ///     Initializes a new instance of <see cref="TrafficListView" />.
    /// </summary>
    public TrafficListView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private async void OnAddCommentClicked(object? sender, RoutedEventArgs routedEventArgs)
    {
        try
        {
            await PromptAndApplyCommentAsync(CancellationToken.None).ConfigureAwait(true);
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
        }
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs eventArgs)
    {
        if (_customColumnSynchronizer is not null)
        {
            return;
        }

        var registry = TrafficListViewServices.ResolveCustomColumnRegistry();
        if (registry is null)
        {
            return;
        }

        var dataGrid = this.FindControl<DataGrid>("FlowGrid");
        if (dataGrid is null)
        {
            return;
        }

        var synchronizer = new CustomColumnGridSynchronizer(dataGrid, registry);
        _customColumnSynchronizer = synchronizer;
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs eventArgs)
    {
        _customColumnSynchronizer?.Detach();
        _customColumnSynchronizer = null;
    }

    private async Task PromptAndApplyCommentAsync(CancellationToken cancellationToken)
    {
        if (DataContext is not TrafficListViewModel viewModel)
        {
            return;
        }

        var selected = viewModel.SelectedFlow;
        if (selected is null)
        {
            return;
        }

        var promptService = TrafficListViewServices.ResolvePromptService();
        if (promptService is null)
        {
            return;
        }

        var localization = TrafficListViewServices.ResolveLocalizationService();
        var title = localization?["Traffic_Context_CommentDialog_Title"] ?? "Comment";
        var label = localization?["Traffic_Context_CommentDialog_Label"] ?? "Comment:";
        var request = new TextPromptRequest
        {
            InitialValue = selected.Comment,
            Label = label,
            Title = title,
        };

        var result = await promptService.PromptAsync(request, cancellationToken).ConfigureAwait(true);
        if (result is null)
        {
            return;
        }

        var trimmed = string.IsNullOrWhiteSpace(result) ? null : result;
        viewModel.ApplyCommentToSelectedCommand.Execute(trimmed);
    }
}
