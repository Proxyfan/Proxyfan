using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Proxyfan.Client.Traffic.Views;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Columns;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="CustomColumnGridSynchronizer" />.
/// </summary>
[NotInParallel]
public sealed class CustomColumnGridSynchronizerTests
{
    static CustomColumnGridSynchronizerTests()
    {
        if (Application.Current is null)
        {
            AppBuilder.Configure<CustomColumnGridSynchronizerHeadlessApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                .SetupWithoutStarting();
        }
    }

    [Test]
    public async Task BuildColumn_ResponseHeaderArrivesAfterCellCreation_RefreshesCellText()
    {
        var definition = new CustomColumnDefinition
        {
            DisplayName = "Content-Type",
            HeaderKey = "Content-Type",
            Id = Guid.NewGuid(),
            Source = CustomColumnSource.Response,
        };
        var registry = new CustomColumnRegistry();
        registry.Add(definition);
        var dataGrid = new DataGrid();
        using var synchronizer = new CustomColumnGridSynchronizerScope(dataGrid, registry);
        var viewModel = new Client.Traffic.ViewModels.TrafficFlowViewModel(CreateRequestEvent(), 1);
        var column = (DataGridTemplateColumn)dataGrid.Columns.Single();
        var cellTemplate = column.CellTemplate;

        await Assert.That(cellTemplate).IsNotNull();

        var cell = (TextBlock)cellTemplate!.Build(viewModel)!;

        await Assert.That(cell.Text).IsEmpty();

        viewModel.UpdateResponse(CreateResponseEvent(viewModel.Id));

        await Assert.That(cell.Text).IsEqualTo("application/json");
    }

    private static RequestReceived CreateRequestEvent()
    {
        var flowId = Guid.NewGuid();
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = "GET",
            RequestUri = new Uri("https://example.com/api/test"),
            Version = "HTTP/1.1",
        });
        return new RequestReceived(flowId, request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
    }

    private static ResponseReceived CreateResponseEvent(Guid flowId)
    {
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Content-Type", "application/json"),
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        return new ResponseReceived(flowId, response, DateTimeOffset.UtcNow);
    }

    private sealed class CustomColumnGridSynchronizerScope : IDisposable
    {
        private readonly CustomColumnGridSynchronizer _synchronizer;

        public CustomColumnGridSynchronizerScope(DataGrid dataGrid, CustomColumnRegistry registry)
        {
            _synchronizer = new CustomColumnGridSynchronizer(dataGrid, registry);
        }

        public void Dispose()
        {
            _synchronizer.Detach();
        }
    }
}

/// <summary>
///     Minimal headless application for custom-column synchronizer tests.
/// </summary>
internal sealed class CustomColumnGridSynchronizerHeadlessApp : Application;
