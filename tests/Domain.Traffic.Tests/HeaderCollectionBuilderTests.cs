using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="HeaderCollection.Builder" />.
/// </summary>
public sealed class HeaderCollectionBuilderTests
{
    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Builder.Build" /> on an empty builder
    ///     produces an empty collection.
    /// </summary>
    [Test]
    public async Task Build_WhenNoHeadersAdded_ReturnsEmptyCollection()
    {
        var builder = new HeaderCollection.Builder();

        var headers = builder.Build();

        await Assert.That(headers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Builder.Add" /> aggregates multiple values
    ///     for the same header name into a single entry.
    /// </summary>
    [Test]
    public async Task Add_WhenSameNameAddedTwice_AggregatesValues()
    {
        var builder = new HeaderCollection.Builder();

        builder.Add("Accept", "application/json");
        builder.Add("Accept", "text/plain");
        var headers = builder.Build();

        await Assert.That(headers.GetAll("Accept").Length).IsEqualTo(2);
        await Assert.That(headers.GetAll("Accept")[0]).IsEqualTo("application/json");
        await Assert.That(headers.GetAll("Accept")[1]).IsEqualTo("text/plain");
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Builder.Add" /> treats header names as
    ///     case-insensitive when aggregating values.
    /// </summary>
    [Test]
    public async Task Add_WhenNamesDifferByCase_AggregatesUnderSingleEntry()
    {
        var builder = new HeaderCollection.Builder();

        builder.Add("Cache-Control", "no-cache");
        builder.Add("cache-control", "no-store");
        var headers = builder.Build();

        await Assert.That(headers.Count).IsEqualTo(1);
        await Assert.That(headers.GetAll("Cache-Control").Length).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that mutations to the builder after <see cref="HeaderCollection.Builder.Build" />
    ///     do not affect a previously built collection.
    /// </summary>
    [Test]
    public async Task Add_AfterBuild_DoesNotMutatePreviousResult()
    {
        var builder = new HeaderCollection.Builder();
        builder.Add("Host", "example.com");

        var snapshot = builder.Build();
        builder.Add("Host", "other.example.com");
        builder.Add("Accept", "text/plain");

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(snapshot.GetAll("Host").Length).IsEqualTo(1);
        await Assert.That(snapshot.Get("Host")).IsEqualTo("example.com");
        await Assert.That(snapshot.HasHeader("Accept")).IsFalse();
    }
}
