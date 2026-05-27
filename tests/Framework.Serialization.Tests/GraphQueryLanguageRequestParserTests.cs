using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="GraphQueryLanguageRequestParser" />.
/// </summary>
public sealed class GraphQueryLanguageRequestParserTests
{
    /// <summary>
    ///     Empty JSON body parses to null.
    /// </summary>
    [Test]
    public async Task FromJson_Empty_ReturnsNull()
    {
        var request = GraphQueryLanguageRequestParser.FromJson(string.Empty);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     JSON containing no <c>query</c> field returns null.
    /// </summary>
    [Test]
    public async Task FromJson_NoQuery_ReturnsNull()
    {
        var request = GraphQueryLanguageRequestParser.FromJson("{\"operationName\":\"X\"}");

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     A full POST body with query, operationName, and variables parses to all fields.
    /// </summary>
    [Test]
    public async Task FromJson_FullBody_ReturnsAllFields()
    {
        var body = "{\"query\":\"query GetUser($id: ID!) { user(id: $id) { name } }\",\"operationName\":\"GetUser\",\"variables\":{\"id\":\"42\"}}";

        var request = GraphQueryLanguageRequestParser.FromJson(body);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Query).Contains("user(id: $id)");
        await Assert.That(request.OperationName).IsEqualTo("GetUser");
        await Assert.That(request.Variables).IsEqualTo("{\"id\":\"42\"}");
    }

    /// <summary>
    ///     When operationName is missing, it is recovered from the query source.
    /// </summary>
    [Test]
    public async Task FromJson_MissingOperationName_RecoveredFromQuery()
    {
        var body = "{\"query\":\"query Viewer { viewer { id } }\"}";

        var request = GraphQueryLanguageRequestParser.FromJson(body);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.OperationName).IsEqualTo("Viewer");
        await Assert.That(request.Variables).IsNull();
    }

    /// <summary>
    ///     Malformed JSON throws InvalidDataException.
    /// </summary>
    [Test]
    public async Task FromJson_Malformed_Throws()
    {
        await Assert.That(() => GraphQueryLanguageRequestParser.FromJson("{not json")).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Raw query body keeps the source verbatim.
    /// </summary>
    [Test]
    public async Task FromRawQuery_NamedQuery_KeepsSourceAndExtractsName()
    {
        var source = "query Foo { x }";

        var request = GraphQueryLanguageRequestParser.FromRawQuery(source);

        await Assert.That(request.Query).IsEqualTo(source);
        await Assert.That(request.OperationName).IsEqualTo("Foo");
        await Assert.That(request.Variables).IsNull();
    }

    /// <summary>
    ///     Empty URL query returns null.
    /// </summary>
    [Test]
    public async Task FromUrlQuery_Empty_ReturnsNull()
    {
        var request = GraphQueryLanguageRequestParser.FromUrlQuery(string.Empty);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     URL query without a 'query' parameter returns null.
    /// </summary>
    [Test]
    public async Task FromUrlQuery_NoQueryParam_ReturnsNull()
    {
        var request = GraphQueryLanguageRequestParser.FromUrlQuery("foo=bar");

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     URL query with encoded query, operationName, and variables parses fully.
    /// </summary>
    [Test]
    public async Task FromUrlQuery_FullParams_ParsesAll()
    {
        var queryString = "query=query%20Foo%20%7B%20x%20%7D&operationName=Foo&variables=%7B%22a%22%3A1%7D";

        var request = GraphQueryLanguageRequestParser.FromUrlQuery(queryString);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Query).IsEqualTo("query Foo { x }");
        await Assert.That(request.OperationName).IsEqualTo("Foo");
        await Assert.That(request.Variables).IsEqualTo("{\"a\":1}");
    }

    /// <summary>
    ///     Variables JSON null is treated as absent.
    /// </summary>
    [Test]
    public async Task FromJson_NullVariables_ReturnsNull()
    {
        var body = "{\"query\":\"{ x }\",\"variables\":null}";

        var request = GraphQueryLanguageRequestParser.FromJson(body);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Variables).IsNull();
    }
}
