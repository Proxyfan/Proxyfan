using System.Threading.Tasks;

namespace Proxyfan.Domain.Tests;

/// <summary>
///     Tests for <see cref="VoidResult" />.
///     Covers success and failure constructor behavior.
/// </summary>
public sealed class VoidResultTests
{
    /// <summary>
    ///     Verifies that <see cref="Result.Success()" /> produces a successful result with no error.
    /// </summary>
    [Test]
    public async Task Success_WhenInvoked_IsSuccessAndNoError()
    {
        var result = Result.Success();

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Error).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="Result.Failure(DomainError)" /> produces an unsuccessful result that
    ///     preserves the supplied <see cref="DomainError" /> so callers never observe a failure with a null error.
    /// </summary>
    [Test]
    public async Task Failure_WithDomainError_IsNotSuccessAndHoldsError()
    {
        var error = new TestError("TEST_CODE", "Test error message");
        var result = Result.Failure(error);

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