using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Rules.Matching;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="BlockListPatternViewModel" />.
/// </summary>
public sealed class BlockListPatternViewModelTests
{
    /// <summary>
    ///     The view model exposes the wrapped rule's pattern, kind, and reference.
    /// </summary>
    [Test]
    public async Task Constructor_ValidRule_ExposesProperties()
    {
        var rule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);

        var viewModel = new BlockListPatternViewModel(rule);

        await Assert.That(viewModel.Pattern).IsEqualTo("https://example.com/*");
        await Assert.That(viewModel.Kind).IsEqualTo(MatchingRuleKind.Wildcard);
        await Assert.That(viewModel.Rule).IsSameReferenceAs(rule);
    }
}
