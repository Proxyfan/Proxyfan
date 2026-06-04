using Proxyfan.Plugin.Abstractions;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="RecordingPluginHost" />.
/// </summary>
public sealed class RecordingPluginHostTests
{
    /// <summary>
    ///     Verifies that the constructor stores the supplied API version.
    /// </summary>
    [Test]
    public async Task Constructor_GivenApiVersion_StoresValue()
    {
        var host = new RecordingPluginHost("1.0");

        await Assert.That(host.ApiVersion).IsEqualTo("1.0");
        await Assert.That(host.InspectorTabs.Count).IsEqualTo(0);
        await Assert.That(host.ContentDecoders.Count).IsEqualTo(0);
        await Assert.That(host.ExportFormatters.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="RecordingPluginHost.RegisterInspectorTab" /> records the
    ///     inspector instances in order.
    /// </summary>
    [Test]
    public async Task RegisterInspectorTab_TwoTabs_RecordsBothInOrder()
    {
        var host = new RecordingPluginHost("1.0");
        var first = new StubTrafficInspector("First");
        var second = new StubTrafficInspector("Second");

        host.RegisterInspectorTab(first);
        host.RegisterInspectorTab(second);

        await Assert.That(host.InspectorTabs.Count).IsEqualTo(2);
        await Assert.That(host.InspectorTabs[0]).IsSameReferenceAs(first);
        await Assert.That(host.InspectorTabs[1]).IsSameReferenceAs(second);
    }

    /// <summary>
    ///     Verifies that <see cref="RecordingPluginHost.RegisterContentDecoder" /> records the
    ///     decoder instance.
    /// </summary>
    [Test]
    public async Task RegisterContentDecoder_GivenDecoder_RecordsInstance()
    {
        var host = new RecordingPluginHost("1.0");
        var decoder = new StubContentDecoder("application/json", "JsonFormatter");

        host.RegisterContentDecoder(decoder);

        await Assert.That(host.ContentDecoders.Count).IsEqualTo(1);
        await Assert.That(host.ContentDecoders[0]).IsSameReferenceAs(decoder);
    }

    /// <summary>
    ///     Verifies that <see cref="RecordingPluginHost.RegisterExportFormatter" /> records the
    ///     formatter instance.
    /// </summary>
    [Test]
    public async Task RegisterExportFormatter_GivenFormatter_RecordsInstance()
    {
        var host = new RecordingPluginHost("1.0");
        var formatter = new StubExportFormatter("CSV");

        host.RegisterExportFormatter(formatter);

        await Assert.That(host.ExportFormatters.Count).IsEqualTo(1);
        await Assert.That(host.ExportFormatters[0]).IsSameReferenceAs(formatter);
    }

    private sealed class StubContentDecoder : IContentDecoder
    {
        public string ContentTypePattern { get; }
        public string Name { get; }

        public StubContentDecoder(string contentTypePattern, string name)
        {
            ContentTypePattern = contentTypePattern;
            Name = name;
        }

        public string Decode(byte[] content)
        {
            return System.Text.Encoding.UTF8.GetString(content);
        }
    }

    private sealed class StubTrafficInspector : ITrafficInspector
    {
        public string DisplayName { get; }
        public int Order { get; }

        public StubTrafficInspector(string displayName)
        {
            DisplayName = displayName;
            Order = 0;
        }
    }

    private sealed class StubExportFormatter : IExportFormatter
    {
        public string DisplayName { get; }

        public StubExportFormatter(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
