using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Rules;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="MapRemoteDestinationFormatter" />.
/// </summary>
public sealed class MapRemoteDestinationFormatterTests
{
    /// <summary>
    ///     A fully-specified destination round-trips through the formatter.
    /// </summary>
    [Test]
    public async Task Format_AllComponentsPresent_ReturnsCompoundString()
    {
        var destination = new MapRemoteDestination("https", "internal.example.com", 8443, "/v2", false);

        var formatted = MapRemoteDestinationFormatter.Format(destination);

        await Assert.That(formatted).IsEqualTo("https://internal.example.com:8443/v2");
    }

    /// <summary>
    ///     Null components are rendered as <c>*</c> to indicate "preserve original".
    /// </summary>
    [Test]
    public async Task Format_NullComponents_RendersAsterisks()
    {
        var destination = new MapRemoteDestination(null, null, null, null, false);

        var formatted = MapRemoteDestinationFormatter.Format(destination);

        await Assert.That(formatted).IsEqualTo("*://*:**");
    }

    /// <summary>
    ///     A partial destination correctly mixes literal and asterisk segments.
    /// </summary>
    [Test]
    public async Task Format_PartialDestination_MixesAsterisksAndValues()
    {
        var destination = new MapRemoteDestination(null, "internal.example.com", null, "/v2", false);

        var formatted = MapRemoteDestinationFormatter.Format(destination);

        await Assert.That(formatted).IsEqualTo("*://internal.example.com:*/v2");
    }
}
