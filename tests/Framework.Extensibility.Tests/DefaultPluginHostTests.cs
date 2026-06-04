using Proxyfan.Plugin.Abstractions;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="DefaultPluginHost" />.
/// </summary>
public sealed class DefaultPluginHostTests
{
    /// <summary>
    ///     Verifies that the host exposes the published API version.
    /// </summary>
    [Test]
    public async Task ApiVersion_AfterConstruction_MatchesPublishedConstant()
    {
        var host = new DefaultPluginHost();

        await Assert.That(host.ApiVersion).IsEqualTo(PluginHostApiVersion.Current);
        await Assert.That(host.ContentDecoderRegistrations.Count).IsEqualTo(0);
        await Assert.That(host.InspectorTabRegistrations.Count).IsEqualTo(0);
        await Assert.That(host.ExportFormatterRegistrations.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that registering a content decoder stores the decoder instance and that
    ///     the registered decoder is consulted (callable) via the registration.
    /// </summary>
    [Test]
    public async Task RegisterContentDecoder_PluginRegistersDecoder_IsConsultedByInspector()
    {
        var host = new DefaultPluginHost();
        var decoder = new StubContentDecoder("application/x-proto", "Protobuf");

        host.RegisterContentDecoder(decoder);

        await Assert.That(host.ContentDecoderRegistrations.Count).IsEqualTo(1);
        await Assert.That(host.ContentDecoderRegistrations[0].Decoder).IsSameReferenceAs(decoder);
        await Assert.That(host.ContentDecoderRegistrations[0].Decoder.ContentTypePattern).IsEqualTo("application/x-proto");
        await Assert.That(host.ContentDecoderRegistrations[0].Decoder.Name).IsEqualTo("Protobuf");
        await Assert.That(host.ContentDecoderRegistrations[0].Decoder.Decode([0x7B, 0x7D])).IsEqualTo("{}");
    }

    /// <summary>
    ///     Verifies that registering an inspector tab stores the inspector instance and that
    ///     it appears with its declared display name.
    /// </summary>
    [Test]
    public async Task RegisterInspectorTab_PluginRegistersTab_AppearsInInspectorWindow()
    {
        var host = new DefaultPluginHost();
        var inspector = new StubTrafficInspector("MyTab", order: 10);

        host.RegisterInspectorTab(inspector);

        await Assert.That(host.InspectorTabRegistrations.Count).IsEqualTo(1);
        await Assert.That(host.InspectorTabRegistrations[0].Inspector).IsSameReferenceAs(inspector);
        await Assert.That(host.InspectorTabRegistrations[0].Inspector.DisplayName).IsEqualTo("MyTab");
        await Assert.That(host.InspectorTabRegistrations[0].Inspector.Order).IsEqualTo(10);
    }

    /// <summary>
    ///     Verifies that registering an export formatter stores the formatter instance.
    /// </summary>
    [Test]
    public async Task RegisterExportFormatter_GivenFormatter_RecordsRegistration()
    {
        var host = new DefaultPluginHost();
        var formatter = new StubExportFormatter("CSV");

        host.RegisterExportFormatter(formatter);

        await Assert.That(host.ExportFormatterRegistrations.Count).IsEqualTo(1);
        await Assert.That(host.ExportFormatterRegistrations[0].Formatter).IsSameReferenceAs(formatter);
        await Assert.That(host.ExportFormatterRegistrations[0].Formatter.DisplayName).IsEqualTo("CSV");
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

        public StubTrafficInspector(string displayName, int order = 0)
        {
            DisplayName = displayName;
            Order = order;
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
