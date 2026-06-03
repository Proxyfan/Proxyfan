using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic.Columns;
using System.Collections.Generic;
using System.ComponentModel;

namespace Proxyfan.Client.Traffic.Views;

/// <summary>
///     Builds template columns from <see cref="CustomColumnDefinition" /> entries and
///     synchronizes them with the supplied Avalonia data grid. Each cell extracts the
///     value of the column's header from the row's underlying traffic flow via
///     <see cref="CustomColumnValueExtractor" />.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Avalonia/host plumbing: requires UI thread/desktop integration, not unit-testable.")]
public sealed class CustomColumnGridSynchronizer
{
    private readonly List<DataGridColumn> _addedColumns;
    private readonly DataGrid _dataGrid;
    private readonly CustomColumnRegistry _registry;
    private bool _isAttached;

    /// <summary>
    ///     Initializes a new synchronizer that subscribes to the supplied registry and
    ///     appends matching columns to the supplied data grid. Columns already in the
    ///     registry at construction are added immediately.
    /// </summary>
    /// <param name="dataGrid">The data grid whose columns are synchronized.</param>
    /// <param name="registry">The registry to observe.</param>
    public CustomColumnGridSynchronizer(DataGrid dataGrid, CustomColumnRegistry registry)
    {
        _dataGrid = dataGrid;
        _registry = registry;
        _addedColumns = [];
        _registry.Changed += OnRegistryChanged;
        _isAttached = true;
        Rebuild();
    }

    /// <summary>
    ///     Stops watching the registry and removes any columns the synchronizer added.
    /// </summary>
    public void Detach()
    {
        if (!_isAttached)
        {
            return;
        }

        _isAttached = false;
        _registry.Changed -= OnRegistryChanged;
        ClearAdded();
    }

    private DataGridTemplateColumn BuildColumn(CustomColumnDefinition definition)
    {
        var template = new FuncDataTemplate<TrafficFlowViewModel>((flow, _) =>
        {
            var margin = new Avalonia.Thickness(4, 0, 4, 0);
            var textBlock = new TextBlock
            {
                Margin = margin,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (flow is null)
            {
                return textBlock;
            }

            void RefreshText()
            {
                textBlock.Text = CustomColumnValueExtractor.Extract(definition, flow.Source);
            }

            void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs eventArgs)
            {
                flow.PropertyChanged -= OnFlowPropertyChanged;
                textBlock.DetachedFromVisualTree -= OnDetachedFromVisualTree;
            }

            void OnFlowPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
            {
                if (eventArgs.PropertyName == nameof(TrafficFlowViewModel.Response))
                {
                    RefreshText();
                }
            }

            RefreshText();
            flow.PropertyChanged += OnFlowPropertyChanged;
            textBlock.DetachedFromVisualTree += OnDetachedFromVisualTree;
            return textBlock;
        });
        var width = new DataGridLength(120);
        var column = new DataGridTemplateColumn
        {
            Header = definition.DisplayName,
            Width = width,
            CellTemplate = template,
        };
        return column;
    }

    private void ClearAdded()
    {
        foreach (var column in _addedColumns)
        {
            _dataGrid.Columns.Remove(column);
        }

        _addedColumns.Clear();
    }

    private void OnRegistryChanged(CustomColumnRegistry sender)
    {
        Rebuild();
    }

    private void Rebuild()
    {
        ClearAdded();
        foreach (var definition in _registry.Snapshot())
        {
            var column = BuildColumn(definition);
            _dataGrid.Columns.Add(column);
            _addedColumns.Add(column);
        }
    }
}
