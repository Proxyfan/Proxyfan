namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parsed result of a single Server-Sent Events field line.
/// </summary>
public sealed class ServerSentEventField
{
    /// <summary>
    ///     Gets the field name (e.g. <c>data</c>, <c>event</c>, <c>id</c>, <c>retry</c>).
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the field value (with any leading space stripped per the spec).
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEventField" />.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="value">The field value.</param>
    public ServerSentEventField(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
