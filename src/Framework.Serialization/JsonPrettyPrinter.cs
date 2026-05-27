using System.Text;
using System.Text.Json;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Pretty-prints JSON text using <see cref="System.Text.Json" />. Returns the original
///     text unchanged when the input is not valid JSON.
/// </summary>
public static class JsonPrettyPrinter
{
    /// <summary>
    ///     Returns a pretty-printed (indented) version of the supplied JSON text. When the
    ///     input is not valid JSON the original text is returned verbatim.
    /// </summary>
    /// <param name="rawJson">The raw JSON text.</param>
    /// <returns>The pretty-printed JSON, or the original text on parse failure.</returns>
    public static string PrettyPrint(string rawJson)
    {
        if (string.IsNullOrEmpty(rawJson))
        {
            return rawJson;
        }

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            using var stream = new System.IO.MemoryStream();
            var options = new JsonWriterOptions
            {
                Indented = true,
            };

            using (var writer = new Utf8JsonWriter(stream, options))
            {
                document.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return rawJson;
        }
    }
}
