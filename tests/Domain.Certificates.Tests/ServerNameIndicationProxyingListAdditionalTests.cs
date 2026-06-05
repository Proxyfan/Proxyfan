using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Additional tests for <see cref="ServerNameIndicationProxyingList" /> covering edge cases.
/// </summary>
public sealed class ServerNameIndicationProxyingListAdditionalTests
{
    /// <summary>
    ///     Verifies that an empty host name returns false even when proxying is enabled.
    /// </summary>
    [Test]
    public async Task HasMatch_EmptyHostname_ReturnsFalse()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("*.example.com");

        await Assert.That(list.HasMatch(string.Empty)).IsFalse();
    }

    /// <summary>
    ///     Verifies that a whitespace host name returns false.
    /// </summary>
    [Test]
    public async Task HasMatch_WhitespaceHostname_ReturnsFalse()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("*.example.com");

        await Assert.That(list.HasMatch("   ")).IsFalse();
    }

    /// <summary>
    ///     Verifies that the wildcard "*" matches any host name when included.
    /// </summary>
    [Test]
    public async Task HasMatch_WildcardStar_MatchesAnyHostname()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("*");

        await Assert.That(list.HasMatch("any.host.test")).IsTrue();
    }

    /// <summary>
    ///     Verifies that a literal host name match returns true (case-insensitive).
    /// </summary>
    [Test]
    public async Task HasMatch_ExactMatch_ReturnsTrue()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("example.com");

        await Assert.That(list.HasMatch("EXAMPLE.COM")).IsTrue();
    }

    /// <summary>
    ///     Verifies that a wildcard pattern (e.g. "*.example.com") does not match the bare suffix
    ///     (e.g. "example.com") — only true sub-domains are matched.
    /// </summary>
    [Test]
    public async Task HasMatch_WildcardDoesNotMatchBareSuffix_ReturnsFalse()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("*.example.com");

        await Assert.That(list.HasMatch("example.com")).IsFalse();
    }

    /// <summary>
    ///     Verifies that a host name with no included pattern returns false.
    /// </summary>
    [Test]
    public async Task HasMatch_NoIncludedPatterns_ReturnsFalse()
    {
        var list = new ServerNameIndicationProxyingList(true);

        await Assert.That(list.HasMatch("example.com")).IsFalse();
    }

    /// <summary>
    ///     Verifies that concurrent edits do not throw while matching host names.
    /// </summary>
    [Test]
    public async Task HasMatch_WhenPatternsAreEditedConcurrently_DoesNotThrow()
    {
        var list = new ServerNameIndicationProxyingList(true);
        list.AddIncludedPattern("*.example.com");

        var errors = new ConcurrentQueue<Exception>();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(300));
        var cancellationToken = cancellationTokenSource.Token;

        var writer = Task.Run(() =>
        {
            var iteration = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var pattern = $"host-{iteration}.example.com";

                try
                {
                    list.AddIncludedPattern(pattern);
                    list.RemoveIncludedPattern(pattern);
                    list.AddExcludedPattern(pattern);
                    list.RemoveExcludedPattern(pattern);

                    if ((iteration & 1) == 0)
                    {
                        list.Enable();
                    }
                    else
                    {
                        list.Disable();
                    }
                }
                catch (Exception exception)
                {
                    errors.Enqueue(exception);
                }

                iteration++;
                Thread.Yield();
            }
        });

        var reader = Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _ = list.HasMatch("api.example.com");
                }
                catch (Exception exception)
                {
                    errors.Enqueue(exception);
                }

                Thread.Yield();
            }
        });

        await Task.WhenAll(writer, reader);

        await Assert.That(errors).IsEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="ServerNameIndicationProxyingList.AddIncludedPattern" />
    ///     rejects a null pattern.
    /// </summary>
    [Test]
    public async Task AddIncludedPattern_NullPattern_Throws()
    {
        var list = new ServerNameIndicationProxyingList(true);

        await Assert.That(() => list.AddIncludedPattern(null!)).Throws<System.ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="ServerNameIndicationProxyingList.AddExcludedPattern" />
    ///     rejects a null pattern.
    /// </summary>
    [Test]
    public async Task AddExcludedPattern_NullPattern_Throws()
    {
        var list = new ServerNameIndicationProxyingList(true);

        await Assert.That(() => list.AddExcludedPattern(null!)).Throws<System.ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="ServerNameIndicationProxyingList.AddIncludedPattern" />
    ///     rejects a whitespace pattern.
    /// </summary>
    [Test]
    public async Task AddIncludedPattern_WhitespacePattern_Throws()
    {
        var list = new ServerNameIndicationProxyingList(true);

        await Assert.That(() => list.AddIncludedPattern("   ")).Throws<System.ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="ServerNameIndicationProxyingList.AddExcludedPattern" />
    ///     rejects a whitespace pattern.
    /// </summary>
    [Test]
    public async Task AddExcludedPattern_WhitespacePattern_Throws()
    {
        var list = new ServerNameIndicationProxyingList(true);

        await Assert.That(() => list.AddExcludedPattern("   ")).Throws<System.ArgumentException>();
    }
}