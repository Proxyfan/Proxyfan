using Avalonia.Controls;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Client.Traffic.Views;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Columns;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="CustomColumnGridSynchronizer" />.
/// </summary>
public sealed class CustomColumnGridSynchronizerTests
{
    /// <summary>
    ///     Verifies a response-backed custom column refreshes after the response is captured.
    /// </summary>
    [Test]
    public async Task Ctor_ResponseColumnBuiltBeforeResponseArrives_RefreshesCellText()
    {
        var registry = new CustomColumnRegistry();
        var definition = new CustomColumnDefinition
        {
            DisplayName = "Request Id",
            HeaderKey = "X-Request-Id",
            Id = Guid.NewGuid(),
            Source = CustomColumnSource.Response,
        };
        registry.Add(definition);

        var dataGrid = new DataGrid();
        var synchronizer = new CustomColumnGridSynchronizer(dataGrid, registry);
        var viewModel = new TrafficFlowViewModel(CreateRequestEvent(), 1);

        var column = (DataGridTemplateColumn)dataGrid.Columns[0];
        var cell = column.CellTemplate!.Build(viewModel);

        await Assert.That(cell).IsNotNull();

        var textBlock = (TextBlock)cell!;

        await Assert.That(textBlock.Text).IsEqualTo(string.Empty);

        viewModel.UpdateResponse(CreateResponseEvent(viewModel.Id));

        await Assert.That(textBlock.Text).IsEqualTo("req-001");

        synchronizer.Detach();
    }

    private static RequestReceived CreateRequestEvent()
    {
        var flowId = Guid.NewGuid();
        var uri = new Uri("https://example.com/api/test");
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = uri,
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        var requestEvent = new RequestReceived(flowId, request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
        return requestEvent;
    }

    private static ResponseReceived CreateResponseEvent(Guid flowId)
    {
        byte[] body = [1, 2, 3];
        var headers = HeaderCollection.Empty.Add("X-Request-Id", "req-001");
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(parameters);
        var responseEvent = new ResponseReceived(flowId, response, DateTimeOffset.UtcNow);
        return responseEvent;
    }
}
