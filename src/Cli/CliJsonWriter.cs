using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli;

/// <summary>
///     Writes machine-readable JSON payloads for CLI automation scenarios.
/// </summary>
public static class CliJsonWriter
{
    private static readonly JsonSerializerOptions JsonOptions;

    static CliJsonWriter()
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        JsonOptions = jsonOptions;
    }

    /// <summary>
    ///     Serializes the supplied value as compact JSON for CLI output.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized JSON string.</returns>
    public static string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    /// <summary>
    ///     Serializes the supplied value as a single JSON line and writes it to the provided writer.
    /// </summary>
    /// <param name="writer">The destination text writer.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes when the JSON line has been written.</returns>
    public static Task WriteLineAsync(TextWriter writer, object value, CancellationToken cancellationToken)
    {
        var json = Serialize(value) + "\n";
        return writer.WriteAsync(json.AsMemory(), cancellationToken);
    }
}
