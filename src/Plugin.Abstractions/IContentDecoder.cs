namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     Decodes raw HTTP body bytes into a human-readable text representation for display
///     in the request/response inspector. Plugins register implementations via
///     <see cref="IPluginHost.RegisterContentDecoder" />.
/// </summary>
public interface IContentDecoder
{
    /// <summary>
    ///     Gets the content-type pattern this decoder handles
    ///     (e.g. <c>"application/json"</c>, <c>"application/x-protobuf"</c>).
    /// </summary>
    string ContentTypePattern { get; }

    /// <summary>
    ///     Gets the short human-readable name shown in the UI decoder selector.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Decodes <paramref name="content" /> into a displayable string.
    /// </summary>
    /// <param name="content">The raw body bytes to decode.</param>
    /// <returns>A human-readable text representation of the body.</returns>
    string Decode(byte[] content);
}
