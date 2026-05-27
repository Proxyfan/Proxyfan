using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Composer;

/// <summary>
///     Mutable builder for <see cref="ComposerRequest" />. Use this when constructing a
///     request incrementally (e.g. from a UI form).
/// </summary>
public sealed class ComposerRequestBuilder
{
    private readonly List<byte> _body;
    private readonly List<ComposerRequestHeader> _headers;
    private string _method;
    private string _url;

    /// <summary>
    ///     Initializes a new empty <see cref="ComposerRequestBuilder" /> (method=GET, url="").
    /// </summary>
    public ComposerRequestBuilder()
    {
        var emptyHeaders = new List<ComposerRequestHeader>();
        _headers = emptyHeaders;
        var emptyBody = new List<byte>();
        _body = emptyBody;
        _method = "GET";
        _url = string.Empty;
    }

    /// <summary>
    ///     Appends a header. Duplicate names are allowed and preserved in order.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>This builder for chaining.</returns>
    public ComposerRequestBuilder AddHeader(string name, string value)
    {
        var header = new ComposerRequestHeader(name, value);
        _headers.Add(header);
        return this;
    }

    /// <summary>
    ///     Materializes a <see cref="ComposerRequest" /> from the current builder state.
    /// </summary>
    /// <returns>The composed request.</returns>
    public ComposerRequest Build()
    {
        var headersCopy = new List<ComposerRequestHeader>(_headers);
        var bodyCopy = new List<byte>(_body);
        var request = new ComposerRequest(_method, _url, headersCopy, bodyCopy);
        return request;
    }

    /// <summary>
    ///     Replaces the request body with the supplied bytes.
    /// </summary>
    /// <param name="body">The body bytes.</param>
    /// <returns>This builder for chaining.</returns>
    public ComposerRequestBuilder SetBody(ReadOnlySpan<byte> body)
    {
        _body.Clear();
        for (var index = 0; index < body.Length; index++)
        {
            _body.Add(body[index]);
        }
        return this;
    }

    /// <summary>
    ///     Sets the HTTP method.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <returns>This builder for chaining.</returns>
    public ComposerRequestBuilder SetMethod(string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        _method = method.ToUpperInvariant();
        return this;
    }

    /// <summary>
    ///     Sets the absolute request URL.
    /// </summary>
    /// <param name="url">The absolute URL.</param>
    /// <returns>This builder for chaining.</returns>
    public ComposerRequestBuilder SetUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        _url = url;
        return this;
    }
}
