using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Tests;

/// <summary>Tests for <see cref="Result{T}" /> and <see cref="Result" />.</summary>
internal sealed class ResultTests
{
    private sealed record TestError(string Code, string Message, Exception? InnerException = null)
        : DomainError(Code, Message, InnerException);

    // ── Result<T> ─────────────────────────────────────────────────────────────

    /// <summary>Verifies that <see cref="Result{T}.IsSuccess" /> is true for a success result.</summary>
    [Test]
    public async Task Generic_Success_IsSuccessIsTrue()
    {
        var result = Result.Success(42);
        await Assert.That(result.IsSuccess).IsTrue();
    }

    /// <summary>Verifies that <see cref="Result{T}.Value" /> returns the provided value on success.</summary>
    [Test]
    public async Task Generic_Success_ValueReturnsProvidedValue()
    {
        var result = Result.Success("hello");
        await Assert.That(result.Value).IsEqualTo("hello");
    }

    /// <summary>Verifies that <see cref="Result{T}.Error" /> is null on success.</summary>
    [Test]
    public async Task Generic_Success_ErrorIsNull()
    {
        var result = Result.Success(1);
        await Assert.That(result.Error).IsNull();
    }

    /// <summary>Verifies that <see cref="Result{T}.IsSuccess" /> is false for a failure result.</summary>
    [Test]
    public async Task Generic_Failure_IsSuccessIsFalse()
    {
        var result = Result.Failure<int>(new TestError("X", "fail"));
        await Assert.That(result.IsSuccess).IsFalse();
    }

    /// <summary>Verifies that <see cref="Result{T}.Error" /> returns the provided error on failure.</summary>
    [Test]
    public async Task Generic_Failure_ErrorReturnsProvidedError()
    {
        var error = new TestError("ERR", "fail");
        var result = Result.Failure<int>(error);
        await Assert.That(result.Error).IsEqualTo(error);
    }

    /// <summary>Verifies that accessing <see cref="Result{T}.Value" /> on a failure throws.</summary>
    [Test]
    public async Task Generic_Failure_AccessingValueThrows()
    {
        var result = Result.Failure<int>(new TestError("X", "fail"));
        await Assert.That(() => result.Value).Throws<InvalidOperationException>();
    }

    // ── Result (non-generic) ─────────────────────────────────────────────────

    /// <summary>Verifies that <see cref="Result.IsSuccess" /> is true for a success result.</summary>
    [Test]
    public async Task NonGeneric_Success_IsSuccessIsTrue()
    {
        var result = Result.Success();
        await Assert.That(result.IsSuccess).IsTrue();
    }

    /// <summary>Verifies that <see cref="Result.Error" /> is null on success.</summary>
    [Test]
    public async Task NonGeneric_Success_ErrorIsNull()
    {
        var result = Result.Success();
        await Assert.That(result.Error).IsNull();
    }

    /// <summary>Verifies that <see cref="Result.IsSuccess" /> is false for a failure result.</summary>
    [Test]
    public async Task NonGeneric_Failure_IsSuccessIsFalse()
    {
        var result = Result.Failure(new TestError("X", "fail"));
        await Assert.That(result.IsSuccess).IsFalse();
    }

    /// <summary>Verifies that <see cref="Result.Error" /> returns the provided error on failure.</summary>
    [Test]
    public async Task NonGeneric_Failure_ErrorReturnsProvidedError()
    {
        var error = new TestError("ERR", "fail");
        var result = Result.Failure(error);
        await Assert.That(result.Error).IsEqualTo(error);
    }

    /// <summary>Verifies that two calls to <see cref="Result.Success()" /> return the same singleton instance.</summary>
    [Test]
    public async Task NonGeneric_SuccessInstances_AreSameReference()
    {
        var a = Result.Success();
        var b = Result.Success();
        await Assert.That(a).IsSameReferenceAs(b);
    }
}
