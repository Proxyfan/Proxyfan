using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="RuleError" />.
/// </summary>
public sealed class RuleErrorTests
{
    /// <summary>
    ///     Verifies that the constructor stores the code and message.
    /// </summary>
    [Test]
    public async Task Constructor_WithValues_StoresCodeAndMessage()
    {
        var error = new RuleError("RULE_INVALID", "The rule is invalid.");

        await Assert.That(error.Code).IsEqualTo("RULE_INVALID");
        await Assert.That(error.Message).IsEqualTo("The rule is invalid.");
    }
}
