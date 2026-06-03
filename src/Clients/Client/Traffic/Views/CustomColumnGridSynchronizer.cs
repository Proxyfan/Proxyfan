using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Proxyfan.Client.Traffic.Converters;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic.Columns;
using System.Collections.Generic;

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
        var converter = new CustomColumnHeaderValueConverter();
        var path = definition.Source == CustomColumnSource.Request
            ? nameof(TrafficFlowViewModel.Request)
            : nameof(TrafficFlowViewModel.Response);
        var binding = new Binding
        {
            Converter = converter,
            ConverterParameter = definition,
            Path = path,
        };
        var template = new FuncDataTemplate<TrafficFlowViewModel>((flow, _) =>
        {
            var margin = new Avalonia.Thickness(4, 0, 4, 0);
            var textBlock = new TextBlock
            {
                DataContext = flow,
                Margin = margin,
                VerticalAlignment = VerticalAlignment.Center,
            };
            textBlock.Bind(TextBlock.TextProperty, binding);
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
