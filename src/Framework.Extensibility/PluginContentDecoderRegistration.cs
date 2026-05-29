namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Records a single content-decoder registration contributed by a plugin via
///     <see cref="Proxyfan.Plugin.Abstractions.IPluginHost.RegisterContentDecoder" />.
/// </summary>
public sealed class PluginContentDecoderRegistration
{
    /// <summary>
    ///     Gets the content-type pattern (e.g. <c>"application/json"</c>) the decoder matches.
    /// </summary>
    public string ContentTypePattern { get; }

    /// <summary>
    ///     Gets the display name of the decoder.
    /// </summary>
    public string DecoderName { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginContentDecoderRegistration" />.
    /// </summary>
    /// <param name="contentTypePattern">The content-type pattern.</param>
    /// <param name="decoderName">The decoder display name.</param>
    public PluginContentDecoderRegistration(string contentTypePattern, string decoderName)
    {
        ContentTypePattern = contentTypePattern;
        DecoderName = decoderName;
    }
}
