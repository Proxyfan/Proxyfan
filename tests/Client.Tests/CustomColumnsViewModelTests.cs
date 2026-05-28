using System.Threading.Tasks;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Traffic.Columns;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="CustomColumnsViewModel" />.
/// </summary>
public sealed class CustomColumnsViewModelTests
{
    /// <summary>
    ///     Verifies that the view model starts empty when the registry is empty.
    /// </summary>
    [Test]
    public async Task Constructor_EmptyRegistry_StartsWithNoColumns()
    {
        var registry = new CustomColumnRegistry();

        using var viewModel = new CustomColumnsViewModel(registry, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.Columns).IsEmpty();
    }

    /// <summary>
    ///     Verifies that pre-existing registry entries are loaded into the view model at construction.
    /// </summary>
    [Test]
    public async Task Constructor_PopulatedRegistry_LoadsExistingColumns()
    {
        var registry = new CustomColumnRegistry();
        registry.Add(new CustomColumnDefinition
        {
            DisplayName = "Trace ID",
            HeaderKey = "X-Trace-Id",
            Id = System.Guid.NewGuid(),
            Source = CustomColumnSource.Request,
        });

        using var viewModel = new CustomColumnsViewModel(registry, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.Columns).Count().IsEqualTo(1);
        await Assert.That(viewModel.Columns[0].DisplayName).IsEqualTo("Trace ID");
        await Assert.That(viewModel.Columns[0].HeaderKey).IsEqualTo("X-Trace-Id");
        await Assert.That(viewModel.Columns[0].Source).IsEqualTo(CustomColumnSource.Request);
    }

    /// <summary>
    ///     Verifies that <c>AddColumnCommand</c> trims whitespace and appends a new column with a new id.
    /// </summary>
    [Test]
    public async Task AddColumnCommand_ValidInput_AppendsTrimmedColumn()
    {
        var registry = new CustomColumnRegistry();
        using var viewModel = new CustomColumnsViewModel(registry, InlineUserInterfaceScheduler.Instance)
        {
            NewColumnDisplayName = "  Request ID  ",
            NewColumnHeaderKey = "  X-Request-Id  ",
            NewColumnSource = CustomColumnSource.Response,
        };

        viewModel.AddColumnCommand.Execute(null);

        await Assert.That(viewModel.Columns).Count().IsEqualTo(1);
        await Assert.That(viewModel.Columns[0].DisplayName).IsEqualTo("Request ID");
        await Assert.That(viewModel.Columns[0].HeaderKey).IsEqualTo("X-Request-Id");
        await Assert.That(viewModel.Columns[0].Source).IsEqualTo(CustomColumnSource.Response);
        await Assert.That(viewModel.NewColumnDisplayName).IsEqualTo(string.Empty);
        await Assert.That(viewModel.NewColumnHeaderKey).IsEqualTo(string.Empty);
        await Assert.That(registry.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that the add command ignores requests with whitespace-only display name.
    /// </summary>
    [Test]
    public async Task AddColumnCommand_WhitespaceDisplayName_DoesNothing()
    {
        var registry = new CustomColumnRegistry();
        using var viewModel = new CustomColumnsViewModel(registry, InlineUserInterfaceScheduler.Instance)
        {
            NewColumnDisplayName = "   ",
            NewColumnHeaderKey = "X-Trace",
        };

        viewModel.AddColumnCommand.Execute(null);

        await Assert.That(viewModel.Columns).IsEmpty();
        await Assert.That(registry.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the add command ignores requests with whitespace-only header key.
    /// </summary>
    [Test]
    public async Task AddColumnCommand_WhitespaceHeaderKey_DoesNothing()
    {
        var registry = new CustomColumnRegistry();
        using var viewModel = new CustomColumnsViewModel(registry, InlineUserInterfaceScheduler.Instance)
        {
            NewColumnDisplayName = "Display",
            NewColumnHeaderKey = " \t",
        };

        viewModel.AddColumnCommand.Execute(null);

        await Assert.That(viewModel.Columns).IsEmpty();
        await Assert.That(registry.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the remove command removes the targeted column.
    /// </summary>
    [Test]
    public async Task RemoveColumnCommand_ExistingEntry_RemovesFromRegistryAndCollection()
    {
        var registry = new CustomColumnRegistry();
        var definition = new CustomColumnDefinition
        {
            DisplayName = "Trace",
            HeaderKey = "X-Trace",
            Id = System.Guid.NewGuid(),
            Source = CustomColumnSource.Request,
        };
        registry.Add(definition);
        using var viewModel = new CustomColumnsViewModel(registry, InlineUserInterfaceScheduler.Instance);
        var entry = viewModel.Columns[0];

        viewModel.RemoveColumnCommand.Execute(entry);

        await Assert.That(viewModel.Columns).IsEmpty();
        await Assert.That(registry.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the remove command tolerates a <c>null</c> argument.
    /// </summary>
    [Test]
    public async Task RemoveColumnCommand_NullEntry_DoesNothing()
    {
        var registry = new CustomColumnRegistry();
        using var viewModel = new CustomColumnsViewModel(registry, InlineUserInterfaceScheduler.Instance);

        viewModel.RemoveColumnCommand.Execute(null);

        await Assert.That(viewModel.Columns).IsEmpty();
    }

    /// <summary>
    ///     Verifies that registry mutations outside the view model are reflected in the collection.
    /// </summary>
    [Test]
    public async Task ExternalRegistryAdd_AddsToRegistry_PublishesChangeIntoCollection()
    {
        var registry = new CustomColumnRegistry();
        using var viewModel = new CustomColumnsViewModel(registry, InlineUserInterfaceScheduler.Instance);

        registry.Add(new CustomColumnDefinition
        {
            DisplayName = "External",
            HeaderKey = "X-External",
            Id = System.Guid.NewGuid(),
            Source = CustomColumnSource.Response,
        });

        await Assert.That(viewModel.Columns).Count().IsEqualTo(1);
        await Assert.That(viewModel.Columns[0].DisplayName).IsEqualTo("External");
    }

    /// <summary>
    ///     Verifies that disposing the view model unsubscribes it from registry changes.
    /// </summary>
    [Test]
    public async Task Dispose_AfterRegistryChange_UnsubscribesFromRegistryChanges()
    {
        var registry = new CustomColumnRegistry();
        var viewModel = new CustomColumnsViewModel(registry, InlineUserInterfaceScheduler.Instance);
        viewModel.Dispose();

        registry.Add(new CustomColumnDefinition
        {
            DisplayName = "Late",
            HeaderKey = "X-Late",
            Id = System.Guid.NewGuid(),
            Source = CustomColumnSource.Request,
        });

        await Assert.That(viewModel.Columns).IsEmpty();
    }
}

