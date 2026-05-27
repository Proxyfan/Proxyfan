using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Proxyfan.Domain.Traffic.Columns;

/// <summary>
///     In-memory mutable list of user-defined custom columns. Raises
///     <see cref="Changed" /> on every mutation so UI layers can rebuild their column
///     templates without polling. Identity is by <see cref="CustomColumnDefinition.Id" />.
/// </summary>
public sealed class CustomColumnRegistry
{
    /// <summary>
    ///     Raised after the registry changes (add, update, or remove).
    /// </summary>
    public event CustomColumnRegistryChanged? Changed;

    private readonly List<CustomColumnDefinition> _columns;

    /// <summary>
    ///     Gets the number of custom columns currently registered.
    /// </summary>
    public int Count => _columns.Count;

    /// <summary>
    ///     Initializes a new empty <see cref="CustomColumnRegistry" />.
    /// </summary>
    public CustomColumnRegistry()
    {
        var columns = new List<CustomColumnDefinition>();
        _columns = columns;
    }

    /// <summary>
    ///     Adds <paramref name="column" /> to the registry. The column's
    ///     <see cref="CustomColumnDefinition.Id" /> must not already exist.
    /// </summary>
    /// <param name="column">The column to add.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a column with the same id is already registered.
    /// </exception>
    public void Add(CustomColumnDefinition column)
    {
        for (var index = 0; index < _columns.Count; index++)
        {
            if (_columns[index].Id == column.Id)
            {
                throw new InvalidOperationException($"A custom column with id '{column.Id}' is already registered.");
            }
        }

        _columns.Add(column);
        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Removes every column from the registry. Raises <see cref="Changed" /> only
    ///     when the registry was non-empty.
    /// </summary>
    public void Clear()
    {
        if (_columns.Count == 0)
        {
            return;
        }

        _columns.Clear();
        Changed?.Invoke(this);
    }

    /// <summary>
    ///     Removes the column with the specified <paramref name="id" />. Absent ids are
    ///     ignored silently.
    /// </summary>
    /// <param name="id">The column id to remove.</param>
    public void Remove(Guid id)
    {
        for (var index = 0; index < _columns.Count; index++)
        {
            if (_columns[index].Id == id)
            {
                _columns.RemoveAt(index);
                Changed?.Invoke(this);
                return;
            }
        }
    }

    /// <summary>
    ///     Returns a read-only snapshot of the registered columns in registration order.
    /// </summary>
    /// <returns>
    ///     A new read-only collection of column definitions.
    /// </returns>
    public ReadOnlyCollection<CustomColumnDefinition> Snapshot()
    {
        var array = new CustomColumnDefinition[_columns.Count];
        for (var index = 0; index < _columns.Count; index++)
        {
            array[index] = _columns[index];
        }

        var snapshot = new ReadOnlyCollection<CustomColumnDefinition>(array);
        return snapshot;
    }

    /// <summary>
    ///     Replaces the existing column having the same id as <paramref name="updated" />.
    /// </summary>
    /// <param name="updated">The updated column definition (with the same id as an existing entry).</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no column with the matching id exists in the registry.
    /// </exception>
    public void Update(CustomColumnDefinition updated)
    {
        for (var index = 0; index < _columns.Count; index++)
        {
            if (_columns[index].Id == updated.Id)
            {
                _columns[index] = updated;
                Changed?.Invoke(this);
                return;
            }
        }

        throw new InvalidOperationException($"No custom column with id '{updated.Id}' is registered.");
    }
}
