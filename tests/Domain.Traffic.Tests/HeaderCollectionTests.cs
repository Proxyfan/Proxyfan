using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="HeaderCollection" />.
/// </summary>
public sealed class HeaderCollectionTests
{
    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Add" /> appends values for an existing header name.
    /// </summary>
    [Test]
    public async Task Add_WhenNameExists_AppendsValue()
    {
        var headers = HeaderCollection.Empty.Add("Accept", "application/json");

        headers = headers.Add("Accept", "text/plain");

        await Assert.That(headers.GetAll("Accept").Length).IsEqualTo(2);
        await Assert.That(headers.GetAll("Accept")[1]).IsEqualTo("text/plain");
    }


    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Add" /> adds a new distinct header name.
    /// </summary>
    [Test]
    public async Task Add_WhenNameIsNew_IncreasesCount()
    {
        var headers = HeaderCollection.Empty;

        headers = headers.Add("Host", "example.com");

        await Assert.That(headers.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Empty" /> contains no headers.
    /// </summary>
    [Test]
    public async Task Empty_WhenAccessed_HasNoHeaders()
    {
        var headers = HeaderCollection.Empty;

        await Assert.That(headers.Count).IsEqualTo(0);
        await Assert.That(headers.GetAll("Missing").Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Get" /> returns the first stored value.
    /// </summary>
    [Test]
    public async Task Get_WhenHeaderExists_ReturnsFirstValue()
    {
        var headers = HeaderCollection.Empty.Add("Accept", "application/json");
        headers = headers.Add("Accept", "text/plain");

        await Assert.That(headers.Get("Accept")).IsEqualTo("application/json");
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.GetAll" /> returns an empty array when absent.
    /// </summary>
    [Test]
    public async Task GetAll_WhenHeaderMissing_ReturnsEmptyArray()
    {
        var headers = HeaderCollection.Empty;
        await Assert.That(headers.GetAll("Missing").Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that enumeration returns the stored headers.
    /// </summary>
    [Test]
    public async Task GetEnumerator_WhenHeadersExist_ReturnsEntries()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var entries = headers.ToList();

        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Key).IsEqualTo("Host");
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.HasHeader" /> ignores header-name casing.
    /// </summary>
    [Test]
    public async Task HasHeader_WhenNameDiffersByCase_ReturnsTrue()
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/json");

        await Assert.That(headers.HasHeader("content-type")).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Add" /> rejects non-token header names.
    /// </summary>
    [Test]
    public async Task Add_WhenNameContainsInvalidTokenCharacter_Throws()
    {
        var headers = HeaderCollection.Empty;

        await Assert.That(() => headers.Add("Bad Name", "value")).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Add" /> rejects CRLF in values.
    /// </summary>
    [Test]
    public async Task Add_WhenValueContainsCarriageReturnLineFeed_Throws()
    {
        var headers = HeaderCollection.Empty;

        await Assert.That(() => headers.Add("X-Test", "a\r\nb")).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Builder.Add" /> enforces the same validation as immutable adds.
    /// </summary>
    [Test]
    public async Task BuilderAdd_WhenValueContainsControlCharacter_Throws()
    {
        var builder = new HeaderCollection.Builder();

        await Assert.That(() => builder.Add("X-Test", "a\u0001b")).Throws<ArgumentException>();
    }
}