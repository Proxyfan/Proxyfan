using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Tests;

/// <summary>Tests for <see cref="DomainError" />.</summary>
internal sealed class DomainErrorTests
{
    private sealed record TestError(string Code, string Message, Exception? InnerException = null)
        : DomainError(Code, Message, InnerException);

    /// <summary>Verifies that <see cref="DomainError.Code" /> is set from the constructor.</summary>
    [Test]
    public async Task Code_WhenConstructed_ReturnsProvidedCode()
    {
        var error = new TestError("TEST_CODE", "test message");
        await Assert.That(error.Code).IsEqualTo("TEST_CODE");
    }

    /// <summary>Verifies that <see cref="DomainError.Message" /> is set from the constructor.</summary>
    [Test]
    public async Task Message_WhenConstructed_ReturnsProvidedMessage()
    {
        var error = new TestError("TEST_CODE", "test message");
        await Assert.That(error.Message).IsEqualTo("test message");
    }

    /// <summary>Verifies that <see cref="DomainError.InnerException" /> defaults to null.</summary>
    [Test]
    public async Task InnerException_WhenNotProvided_IsNull()
    {
        var error = new TestError("TEST_CODE", "test message");
        await Assert.That(error.InnerException).IsNull();
    }

    /// <summary>Verifies that <see cref="DomainError.InnerException" /> is set when provided.</summary>
    [Test]
    public async Task InnerException_WhenProvided_ReturnsProvidedException()
    {
        var inner = new InvalidOperationException("inner");
        var error = new TestError("TEST_CODE", "test message", inner);
        await Assert.That(error.InnerException).IsSameReferenceAs(inner);
    }

    /// <summary>Verifies that two errors with identical values are equal (record equality).</summary>
    [Test]
    public async Task Equality_SameValues_AreEqual()
    {
        var a = new TestError("X", "msg");
        var b = new TestError("X", "msg");
        await Assert.That(a).IsEqualTo(b);
    }

    /// <summary>Verifies that errors with different codes are not equal.</summary>
    [Test]
    public async Task Equality_DifferentCodes_AreNotEqual()
    {
        var a = new TestError("A", "msg");
        var b = new TestError("B", "msg");
        await Assert.That(a).IsNotEqualTo(b);
    }
}
