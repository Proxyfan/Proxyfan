using System.Threading.Tasks;

namespace Proxyfan.Domain.Tests;

/// <summary>
///     Tests for <see cref="VoidResult" />.
///     Covers the parameterless success constructor and the <see cref="DomainError" /> failure
///     constructor, ensuring a failed result always carries its error.
/// </summary>
public sealed class VoidResultTests
{
    /// <summary>
    ///     Verifies that the parameterless <see cref="VoidResult" /> constructor produces a
    ///     successful result with no error.
    /// </summary>
    [Test]
    public async Task Constructor_Parameterless_IsSuccessAndNoError()
    {
        var result = new VoidResult();

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Error).IsNull();
    }

    /// <summary>
    ///     Verifies that the <see cref="DomainError" /> constructor produces an unsuccessful
    ///     result that preserves the supplied error so callers never observe a failure with a
    ///     null error.
    /// </summary>
    [Test]
    public async Task Constructor_WithDomainError_IsNotSuccessAndHoldsError()
    {
        var error = new TestError("TEST_CODE", "Test error message");
        var result = new VoidResult(error);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsSameReferenceAs(error);
    }

    private sealed record TestError : DomainError
    {
        public TestError(string code, string message)
            : base(code, message)
        {
        }
    }
}