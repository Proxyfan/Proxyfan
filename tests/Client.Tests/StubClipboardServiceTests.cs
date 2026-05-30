using Proxyfan.Client.Tests.Stubs;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for the test-only <see cref="StubClipboardService" /> stub itself. This protects
///     us from a stub-bug masking failures in the production view-model tests that rely on
///     the stub recording behaviour faithfully.
/// </summary>
public sealed class StubClipboardServiceTests
{
    /// <summary>
    ///     Verifies that <see cref="StubClipboardService.SetTextAsync" /> records non-empty
    ///     text and returns success.
    /// </summary>
    [Test]
    public async Task SetTextAsync_NonEmptyText_RecordsAndReturnsTrue()
    {
        var stub = new StubClipboardService();

        var ok = await stub.SetTextAsync("hello", CancellationToken.None);

        await Assert.That(ok).IsTrue();
        await Assert.That(stub.CopiedTexts.Count).IsEqualTo(1);
        await Assert.That(stub.CopiedTexts[0]).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that null and empty text are treated as a no-op (false return, nothing
    ///     recorded).
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task SetTextAsync_NullOrEmptyText_ReturnsFalseAndDoesNotRecord(string? text)
    {
        var stub = new StubClipboardService();

        var ok = await stub.SetTextAsync(text, CancellationToken.None);

        await Assert.That(ok).IsFalse();
        await Assert.That(stub.CopiedTexts.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the <see cref="StubClipboardService.ShouldFail" /> flag causes
    ///     <see cref="StubClipboardService.SetTextAsync" /> to return false without recording.
    /// </summary>
    [Test]
    public async Task SetTextAsync_ShouldFailEnabled_ReturnsFalseAndDoesNotRecord()
    {
        var stub = new StubClipboardService
        {
            ShouldFail = true,
        };

        var ok = await stub.SetTextAsync("something", CancellationToken.None);

        await Assert.That(ok).IsFalse();
        await Assert.That(stub.CopiedTexts.Count).IsEqualTo(0);
    }
}
