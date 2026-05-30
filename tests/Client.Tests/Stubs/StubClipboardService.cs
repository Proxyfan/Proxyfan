using Proxyfan.Presentation.Clipboard;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Hand-written stub implementation of <see cref="IClipboardService" /> that records each
///     copy operation so tests can assert what was sent to the clipboard. The
///     <see cref="ShouldFail" /> flag flips <see cref="SetTextAsync" /> into a failure mode for
///     negative-path testing.
/// </summary>
public sealed class StubClipboardService : IClipboardService
{
    private readonly List<string?> _copiedTexts;

    /// <summary>
    ///     Gets the list of values successfully copied to the stubbed clipboard, in order.
    /// </summary>
    public IReadOnlyList<string?> CopiedTexts => _copiedTexts;

    /// <summary>
    ///     Gets or sets a value indicating whether <see cref="SetTextAsync" /> should report
    ///     a failure to the caller (without throwing) — simulates the case where the platform
    ///     clipboard is unavailable.
    /// </summary>
    public bool ShouldFail { get; set; }

    /// <summary>
    ///     Initializes a new <see cref="StubClipboardService" />.
    /// </summary>
    public StubClipboardService()
    {
        var copiedTexts = new List<string?>();
        _copiedTexts = copiedTexts;
    }

    /// <inheritdoc />
    public Task<bool> SetTextAsync(string? text, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (ShouldFail)
        {
            return Task.FromResult(false);
        }

        if (string.IsNullOrEmpty(text))
        {
            return Task.FromResult(false);
        }

        _copiedTexts.Add(text);
        return Task.FromResult(true);
    }
}
