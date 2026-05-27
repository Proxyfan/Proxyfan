using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="GraphQueryLanguageOperationNameExtractor" />.
/// </summary>
public sealed class GraphQueryLanguageOperationNameExtractorTests
{
    /// <summary>
    ///     Anonymous queries have no operation name.
    /// </summary>
    [Test]
    public async Task Extract_AnonymousQuery_ReturnsNull()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("{ viewer { id } }");

        await Assert.That(name).IsNull();
    }

    /// <summary>
    ///     Empty input returns null.
    /// </summary>
    [Test]
    public async Task Extract_Empty_ReturnsNull()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract(string.Empty);

        await Assert.That(name).IsNull();
    }

    /// <summary>
    ///     Mutation operation names are extracted.
    /// </summary>
    [Test]
    public async Task Extract_MutationWithName_ReturnsName()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("mutation CreateUser($input: UserInput!) { createUser(input: $input) { id } }");

        await Assert.That(name).IsEqualTo("CreateUser");
    }

    /// <summary>
    ///     Leading comments and whitespace are skipped.
    /// </summary>
    [Test]
    public async Task Extract_QueryWithLeadingComment_ReturnsName()
    {
        var source = "# leading comment\n  query  Viewer  { viewer { id } }";

        var name = GraphQueryLanguageOperationNameExtractor.Extract(source);

        await Assert.That(name).IsEqualTo("Viewer");
    }

    /// <summary>
    ///     Named queries return the operation name.
    /// </summary>
    [Test]
    public async Task Extract_QueryWithName_ReturnsName()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("query GetUser($id: ID!) { user(id: $id) { name } }");

        await Assert.That(name).IsEqualTo("GetUser");
    }

    /// <summary>
    ///     Subscription operation names are extracted.
    /// </summary>
    [Test]
    public async Task Extract_SubscriptionWithName_ReturnsName()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("subscription OnUserChanged { userChanged { id } }");

        await Assert.That(name).IsEqualTo("OnUserChanged");
    }

    /// <summary>
    ///     A bare keyword without a following identifier returns null.
    /// </summary>
    [Test]
    public async Task Extract_KeywordWithoutName_ReturnsNull()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("query { viewer { id } }");

        await Assert.That(name).IsNull();
    }

    /// <summary>
    ///     Identifier-like prefixes that share a keyword's beginning are not treated as keywords.
    /// </summary>
    [Test]
    public async Task Extract_QueryishPrefix_IsNotMistakenForKeyword()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("queryMaker GetUser { user { id } }");

        await Assert.That(name).IsNull();
    }
}
