namespace Proxyfan.Domain.Traffic;

/// <summary>
///     A parsed Graph Query Language (GraphQL) request as transmitted over HTTP
///     (POST application/json, GET, or POST application/graphql). Captures the query source,
///     optional operation name, and JSON-encoded variables (preserved verbatim so the
///     original formatting survives).
/// </summary>
public sealed record GraphQueryLanguageRequest
{
    /// <summary>
    ///     Gets the explicit operation name from the request, or the first named operation
    ///     extracted from <see cref="Query" /> when the request did not include one.
    ///     <see langword="null" /> when the document contains a single anonymous operation.
    /// </summary>
    public string? OperationName { get; }

    /// <summary>
    ///     Gets the query, mutation, or subscription source text.
    /// </summary>
    public string Query { get; }

    /// <summary>
    ///     Gets the raw JSON text of the variables object, or <see langword="null" /> when no
    ///     variables were sent. The JSON is preserved verbatim - callers may parse it as needed.
    /// </summary>
    public string? Variables { get; }

    /// <summary>
    ///     Initializes a new <see cref="GraphQueryLanguageRequest" />.
    /// </summary>
    /// <param name="query">The query source.</param>
    /// <param name="operationName">The operation name, or <see langword="null" />.</param>
    /// <param name="variables">The raw JSON variables, or <see langword="null" />.</param>
    public GraphQueryLanguageRequest(string query, string? operationName, string? variables)
    {
        Query = query;
        OperationName = operationName;
        Variables = variables;
    }
}
