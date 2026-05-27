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
    }

    /// <summary>
    ///     Verifies that <see cref="RecordingPluginHost.RegisterInspectorTab" /> records the
    ///     tab name in order.
    /// </summary>
    [Test]
    public async Task RegisterInspectorTab_TwoTabs_RecordsBothInOrder()
    {
        var host = new RecordingPluginHost("1.0");

        host.RegisterInspectorTab("First");
        host.RegisterInspectorTab("Second");

        await Assert.That(host.InspectorTabs.Count).IsEqualTo(2);
        await Assert.That(host.InspectorTabs[0]).IsEqualTo("First");
        await Assert.That(host.InspectorTabs[1]).IsEqualTo("Second");
    }

    /// <summary>
    ///     Verifies that <see cref="RecordingPluginHost.RegisterContentDecoder" /> formats the
    ///     entry as "pattern => decoderName".
    /// </summary>
    [Test]
    public async Task RegisterContentDecoder_GivenPattern_FormatsAsArrowExpression()
    {
        var host = new RecordingPluginHost("1.0");

        host.RegisterContentDecoder("application/json", "JsonFormatter");

        await Assert.That(host.ContentDecoders.Count).IsEqualTo(1);
        await Assert.That(host.ContentDecoders[0]).IsEqualTo("application/json => JsonFormatter");
    }
}
