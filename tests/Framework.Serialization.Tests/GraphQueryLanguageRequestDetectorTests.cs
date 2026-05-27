using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="GraphQueryLanguageRequestDetector" />.
/// </summary>
public sealed class GraphQueryLanguageRequestDetectorTests
{
    /// <summary>
    ///     A URL ending in /graphql is detected.
    /// </summary>
    [Test]
    public async Task HasIndicator_GraphQlPath_ReturnsTrue()
    {
        var has = GraphQueryLanguageRequestDetector.HasIndicator("/graphql", null);

        await Assert.That(has).IsTrue();
    }

    /// <summary>
    ///     URLs containing /graphql in a sub-path are detected.
    /// </summary>
    [Test]
    public async Task HasIndicator_ApiGraphQlPath_ReturnsTrue()
    {
        var has = GraphQueryLanguageRequestDetector.HasIndicator("/api/graphql", null);

        await Assert.That(has).IsTrue();
    }

    /// <summary>
    ///     Unrelated paths are not detected.
    /// </summary>
    [Test]
    public async Task HasIndicator_UnrelatedPath_ReturnsFalse()
    {
        var has = GraphQueryLanguageRequestDetector.HasIndicator("/users", null);

        await Assert.That(has).IsFalse();
    }

    /// <summary>
    ///     application/graphql is detected by content-type alone.
    /// </summary>
    [Test]
    public async Task HasIndicator_ApplicationGraphQlContentType_ReturnsTrue()
    {
        var has = GraphQueryLanguageRequestDetector.HasIndicator("/api/data", "application/graphql");

        await Assert.That(has).IsTrue();
    }

    /// <summary>
    ///     application/graphql-response+json is detected.
    /// </summary>
    [Test]
    public async Task HasIndicator_GraphQlResponseJsonContentType_ReturnsTrue()
    {
        var has = GraphQueryLanguageRequestDetector.HasIndicator("/api/data", "application/graphql-response+json; charset=utf-8");

        await Assert.That(has).IsTrue();
    }

    /// <summary>
    ///     application/json (ambiguous) is NOT detected by content-type alone.
    /// </summary>
    [Test]
    public async Task HasIndicator_PlainJson_ReturnsFalse()
    {
        var has = GraphQueryLanguageRequestDetector.HasIndicator("/api/data", "application/json");

        await Assert.That(has).IsFalse();
    }

    /// <summary>
    ///     Null arguments are treated as "no indicator".
    /// </summary>
    [Test]
    public async Task HasIndicator_NullPathAndType_ReturnsFalse()
    {
        var has = GraphQueryLanguageRequestDetector.HasIndicator(null, null);

        await Assert.That(has).IsFalse();
    }

    /// <summary>
    ///     URL query string is ignored for path matching.
    /// </summary>
    [Test]
    public async Task HasIndicator_PathWithQueryString_StillDetects()
    {
        var has = GraphQueryLanguageRequestDetector.HasIndicator("/graphql?op=Foo", null);

        await Assert.That(has).IsTrue();
    }
}
