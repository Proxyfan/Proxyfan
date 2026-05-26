using System.Threading.Tasks;

namespace Proxyfan.Domain.Tests;

/// <summary>
///     Tests for <see cref="VoidResult" />.
///     Covers success and failure constructor behavior.
/// </summary>
public sealed class VoidResultTests
{
    /// <summary>
    ///     Verifies that constructing with <see langword="true" /> marks the result successful.
    ///     Also verifies that a successful result has no associated error.
    /// </summary>
    [Test]
    public async Task Constructor_WithTrue_IsSuccessAndNoError()
    {
        var result = new VoidResult(true);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Error).IsNull();
    }

    /// <summary>
    ///     Verifies that constructing with <see langword="false" /> marks the result unsuccessful.
    ///     Also verifies that the boolean-failure overload leaves the error unset.
    /// </summary>
    [Test]
    public async Task Constructor_WithFalse_IsNotSuccessAndNoError()
    {
        var result = new VoidResult(false);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsNull();
    }

    /// <summary>
    ///     Verifies that constructing with a <see cref="DomainError" /> marks the result unsuccessful.
    ///     Also verifies that the provided error instance is preserved.
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