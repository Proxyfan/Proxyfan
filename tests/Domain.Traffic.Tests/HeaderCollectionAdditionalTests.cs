using System.Collections;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Additional tests for <see cref="HeaderCollection" /> covering edge cases.
/// </summary>
public sealed class HeaderCollectionAdditionalTests
{
    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.Get" /> returns null for a missing header.
    /// </summary>
    [Test]
    public async Task Get_WhenMissing_ReturnsNull()
    {
        var headers = HeaderCollection.Empty;

        await Assert.That(headers.Get("Missing")).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HeaderCollection.HasHeader" /> returns false when missing.
    /// </summary>
    [Test]
    public async Task HasHeader_WhenMissing_ReturnsFalse()
    {
        var headers = HeaderCollection.Empty;

        await Assert.That(headers.HasHeader("Missing")).IsFalse();
    }

    /// <summary>
    ///     Verifies that the non-generic enumerator works.
    /// </summary>
    [Test]
    public async Task NonGenericEnumerator_WhenHeadersExist_IteratesEntries()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com").Add("Accept", "*/*");
        IEnumerable enumerable = headers;
        var count = 0;

        foreach (var _ in enumerable)
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that returned arrays from <see cref="HeaderCollection.GetAll" /> are defensive copies.
    /// </summary>
    [Test]
    public async Task GetAll_WhenReturnedArrayMutated_DoesNotChangeStoredHeader()
    {
        var headers = HeaderCollection.Empty.Add("Accept", "application/json");
        var values = headers.GetAll("Accept");

        values[0] = "text/plain";

        await Assert.That(headers.Get("Accept")).IsEqualTo("application/json");
    }
}
