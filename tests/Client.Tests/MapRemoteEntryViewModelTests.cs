using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="MapRemoteEntryViewModel" />.
/// </summary>
public sealed class MapRemoteEntryViewModelTests
{
    /// <summary>
    ///     The view model exposes the domain pattern, kind, formatted destination, and underlying entry.
    /// </summary>
    [Test]
    public async Task Constructor_FromEntry_ExposesProperties()
    {
        var matchingRule = new MatchingRule("https://example.com/api/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination("https", "api.internal", 8443, "/v2", true);
        var entry = new MapRemoteEntry { Destination = destination, IsEnabled = true, MatchingRule = matchingRule };

        var viewModel = new MapRemoteEntryViewModel(entry);

        await Assert.That(viewModel.Pattern).IsEqualTo("https://example.com/api/*");
        await Assert.That(viewModel.Kind).IsEqualTo(MatchingRuleKind.Wildcard);
        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(viewModel.Destination).IsEqualTo("https://api.internal:8443/v2");
        await Assert.That(viewModel.Entry).IsSameReferenceAs(entry);
    }
}
