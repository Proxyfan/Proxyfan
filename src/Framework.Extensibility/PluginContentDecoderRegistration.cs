using Proxyfan.Plugin.Abstractions;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Records a single content-decoder registration contributed by a plugin via
///     <see cref="Proxyfan.Plugin.Abstractions.IPluginHost.RegisterContentDecoder" />.
/// </summary>
public sealed class PluginContentDecoderRegistration
{
    /// <summary>
    ///     Gets the decoder implementation contributed by the plugin.
    /// </summary>
    public IContentDecoder Decoder { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginContentDecoderRegistration" />.
    /// </summary>
    /// <param name="decoder">The decoder implementation.</param>
    public PluginContentDecoderRegistration(IContentDecoder decoder)
    {
        Decoder = decoder;
    }
}
