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

    /// <summary>
    ///     Null input returns null without throwing.
    /// </summary>
    [Test]
    public async Task Extract_Null_ReturnsNull()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract(null);

        await Assert.That(name).IsNull();
    }

    /// <summary>
    ///     A source consisting only of whitespace and comments returns null.
    /// </summary>
    [Test]
    public async Task Extract_OnlyCommentsAndWhitespace_ReturnsNull()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("   \n# only a comment\n\t");

        await Assert.That(name).IsNull();
    }

    /// <summary>
    ///     Commas (which GraphQL treats as insignificant whitespace) are skipped between tokens.
    /// </summary>
    [Test]
    public async Task Extract_CommasBetweenTokens_AreSkippedAsWhitespace()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract(",,, query,,, GetUser { user { id } }");

        await Assert.That(name).IsEqualTo("GetUser");
    }

    /// <summary>
    ///     A keyword followed only by trailing whitespace (no identifier afterwards) returns null.
    /// </summary>
    [Test]
    public async Task Extract_KeywordFollowedByOnlyWhitespace_ReturnsNull()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("query   ");

        await Assert.That(name).IsNull();
    }

    /// <summary>
    ///     Operation names containing digits (after the first character) are accepted.
    /// </summary>
    [Test]
    public async Task Extract_OperationNameWithDigits_ReturnsFullName()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("query GetUser123 { user { id } }");

        await Assert.That(name).IsEqualTo("GetUser123");
    }

    /// <summary>
    ///     A keyword immediately followed by a digit (which is a valid identifier part char)
    ///     is rejected — it forms a longer identifier rather than a keyword.
    /// </summary>
    [Test]
    public async Task Extract_KeywordFollowedByDigit_IsNotMistakenForKeyword()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("query0 GetUser { user { id } }");

        await Assert.That(name).IsNull();
    }

    /// <summary>
    ///     An operation name that starts with an underscore is accepted.
    /// </summary>
    [Test]
    public async Task Extract_OperationNameStartingWithUnderscore_ReturnsFullName()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("query _PrivateOp { viewer { id } }");

        await Assert.That(name).IsEqualTo("_PrivateOp");
    }

    /// <summary>
    ///     A single-character operation name is accepted (exercises the no-loop branch of
    ///     <c>TryReadIdentifier</c>'s tail loop).
    /// </summary>
    [Test]
    public async Task Extract_SingleCharacterOperationName_ReturnsFullName()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("query A { viewer { id } }");

        await Assert.That(name).IsEqualTo("A");
    }

    /// <summary>
    ///     A trailing comment with no newline at end of input is skipped to end-of-span.
    /// </summary>
    [Test]
    public async Task Extract_CommentRunsToEndOfInput_IsTreatedAsWhitespace()
    {
        var name = GraphQueryLanguageOperationNameExtractor.Extract("# trailing comment that never ends");

        await Assert.That(name).IsNull();
    }

    /// <summary>
    ///     A fragment definition preceding the named operation is skipped.
    /// </summary>
    [Test]
    public async Task Extract_FragmentBeforeNamedQuery_ReturnsOperationName()
    {
        var source = "fragment UserFields on User { id name } query GetUser { user { ...UserFields } }";

        var name = GraphQueryLanguageOperationNameExtractor.Extract(source);

        await Assert.That(name).IsEqualTo("GetUser");
    }

    /// <summary>
    ///     Multiple fragment definitions before the operation are all skipped.
    /// </summary>
    [Test]
    public async Task Extract_MultipleFragmentsBeforeMutation_ReturnsOperationName()
    {
        var source = "fragment A on T { x } fragment B on T { y } mutation DoIt { doIt { ok } }";

        var name = GraphQueryLanguageOperationNameExtractor.Extract(source);

        await Assert.That(name).IsEqualTo("DoIt");
    }

    /// <summary>
    ///     Fragments containing nested braces are skipped without losing the operation name.
    /// </summary>
    [Test]
    public async Task Extract_FragmentWithNestedSelectionSet_ReturnsOperationName()
    {
        var source = "fragment Deep on Node { children { children { id } } } query Outer { root { ...Deep } }";

        var name = GraphQueryLanguageOperationNameExtractor.Extract(source);

        await Assert.That(name).IsEqualTo("Outer");
    }

    /// <summary>
    ///     Braces appearing inside string arguments within a fragment do not unbalance the scanner.
    /// </summary>
    [Test]
    public async Task Extract_FragmentWithStringContainingBrace_ReturnsOperationName()
    {
        var source = "fragment F on T { field(arg: \"value with } brace\") } query AfterFragment { x }";

        var name = GraphQueryLanguageOperationNameExtractor.Extract(source);

        await Assert.That(name).IsEqualTo("AfterFragment");
    }

    /// <summary>
    ///     A fragment definition followed by an anonymous operation still has no operation name.
    /// </summary>
    [Test]
    public async Task Extract_FragmentBeforeAnonymousOperation_ReturnsNull()
    {
        var source = "fragment F on T { id } { viewer { ...F } }";

        var name = GraphQueryLanguageOperationNameExtractor.Extract(source);

        await Assert.That(name).IsNull();
    }
}
