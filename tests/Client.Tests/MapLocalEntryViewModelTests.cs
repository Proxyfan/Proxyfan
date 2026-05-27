using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="MapLocalEntryViewModel" />.
/// </summary>
public sealed class MapLocalEntryViewModelTests
{
    /// <summary>
    ///     The view model exposes the domain pattern, kind, status text, and underlying entry.
    /// </summary>
    [Test]
    public async Task Constructor_FromEntry_ExposesProperties()
    {
        var matchingRule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        var entry = new MapLocalEntry
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = new List<KeyValuePair<string, string>>(),
            IsEnabled = true,
            MatchingRule = matchingRule,
            ReasonPhrase = "Created",
            StatusCode = 201,
        };

        var viewModel = new MapLocalEntryViewModel(entry);

        await Assert.That(viewModel.Pattern).IsEqualTo("https://example.com/*");
        await Assert.That(viewModel.Kind).IsEqualTo(MatchingRuleKind.Wildcard);
        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(viewModel.Status).IsEqualTo("201 Created");
        await Assert.That(viewModel.Entry).IsSameReferenceAs(entry);
    }
}
