using System;
using System.Threading.Tasks;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Traffic.Columns;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="CustomColumnEntryViewModel" />.
/// </summary>
public sealed class CustomColumnEntryViewModelTests
{
    /// <summary>
    ///     Verifies that the view model copies all surface properties from the source definition.
    /// </summary>
    [Test]
    public async Task Constructor_Definition_ProjectsAllProperties()
    {
        var id = Guid.NewGuid();
        var definition = new CustomColumnDefinition
        {
            DisplayName = "Trace ID",
            HeaderKey = "X-Trace-Id",
            Id = id,
            Source = CustomColumnSource.Response,
        };

        var viewModel = new CustomColumnEntryViewModel(definition);

        await Assert.That(viewModel.DisplayName).IsEqualTo("Trace ID");
        await Assert.That(viewModel.HeaderKey).IsEqualTo("X-Trace-Id");
        await Assert.That(viewModel.Source).IsEqualTo(CustomColumnSource.Response);
        await Assert.That(viewModel.Definition).IsSameReferenceAs(definition);
    }
}
