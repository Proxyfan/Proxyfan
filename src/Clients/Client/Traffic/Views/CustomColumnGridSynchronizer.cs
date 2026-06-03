using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Columns;
using System;
using System.Collections.Generic;
using System.Globalization;

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
        var converter = new CustomColumnValueConverter(definition);
        var binding = new Binding(nameof(TrafficFlowViewModel.Source))
        {
            Converter = converter,
            Mode = BindingMode.OneWay,
        };
        var template = new FuncDataTemplate<TrafficFlowViewModel>((flow, _) =>
        {
            var margin = new Avalonia.Thickness(4, 0, 4, 0);
            var textBlock = new TextBlock
            {
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

    /// <summary>
    ///     Converts a bound <see cref="TrafficFlow" /> into the display text for one custom column.
    /// </summary>
    private sealed class CustomColumnValueConverter : IValueConverter
    {
        private readonly CustomColumnDefinition _definition;

        /// <summary>
        ///     Initializes a converter for a specific custom column definition.
        /// </summary>
        /// <param name="definition">The custom column definition to evaluate for each row.</param>
        public CustomColumnValueConverter(CustomColumnDefinition definition)
        {
            _definition = definition;
        }

        /// <summary>
        ///     Computes the cell text for the bound traffic-flow row.
        /// </summary>
        /// <param name="value">The bound value, expected to be a <see cref="TrafficFlow" />.</param>
        /// <param name="targetType">The destination type requested by the binding engine.</param>
        /// <param name="parameter">Optional converter parameter supplied by the binding.</param>
        /// <param name="culture">The culture requested by the binding engine.</param>
        /// <returns>The resolved custom-column value, or an empty string when unavailable.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not TrafficFlow flow)
            {
                return string.Empty;
            }

            return CustomColumnValueExtractor.Extract(_definition, flow);
        }

        /// <summary>
        ///     Reverse conversion is not supported.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The desired source type.</param>
        /// <param name="parameter">Optional converter parameter.</param>
        /// <param name="culture">The requested culture.</param>
        /// <returns>This method never returns a value.</returns>
        /// <exception cref="NotSupportedException">Always thrown because reverse conversion is unsupported.</exception>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
