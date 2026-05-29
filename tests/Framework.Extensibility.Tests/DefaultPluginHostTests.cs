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
    }

    /// <summary>
    ///     Verifies that registering an inspector tab is recorded.
    /// </summary>
    [Test]
    public async Task RegisterInspectorTab_GivenName_RecordsRegistration()
    {
        var host = new DefaultPluginHost();

        host.RegisterInspectorTab("MyTab");

        await Assert.That(host.InspectorTabRegistrations.Count).IsEqualTo(1);
        await Assert.That(host.InspectorTabRegistrations[0].TabName).IsEqualTo("MyTab");
    }

    /// <summary>
    ///     Verifies that registering a content decoder is recorded.
    /// </summary>
    [Test]
    public async Task RegisterContentDecoder_GivenPatternAndName_RecordsRegistration()
    {
        var host = new DefaultPluginHost();

        host.RegisterContentDecoder("application/x-proto", "Protobuf");

        await Assert.That(host.ContentDecoderRegistrations.Count).IsEqualTo(1);
        await Assert.That(host.ContentDecoderRegistrations[0].ContentTypePattern).IsEqualTo("application/x-proto");
        await Assert.That(host.ContentDecoderRegistrations[0].DecoderName).IsEqualTo("Protobuf");
    }
}
