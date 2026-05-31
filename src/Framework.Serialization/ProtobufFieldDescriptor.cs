namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Describes a single field of a protobuf message as declared in a <c>.proto</c> file
///     (subset of <c>google.protobuf.FieldDescriptorProto</c>).
/// </summary>
public sealed class ProtobufFieldDescriptor
{
    /// <summary>
    ///     Gets the field's intrinsic kind (scalar/message/enum/etc.).
    /// </summary>
    public required ProtobufFieldKind Kind { get; init; }

    /// <summary>
    ///     Gets the field multiplicity (optional, required, repeated).
    /// </summary>
    public required ProtobufFieldLabel Label { get; init; }

    /// <summary>
    ///     Gets the field name as declared in the <c>.proto</c> file.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the field number (the integer tag).
    /// </summary>
    public required int Number { get; init; }

    /// <summary>
    ///     Gets the fully qualified name of the referenced message or enum type when
    ///     <see cref="Kind" /> is <see cref="ProtobufFieldKind.TypeMessage" /> or
    ///     <see cref="ProtobufFieldKind.TypeEnum" />. <see langword="null" /> for scalar fields.
    ///     The leading dot from the wire descriptor is preserved (e.g. <c>".foo.Bar"</c>).
    /// </summary>
    public string? TypeName { get; init; }
}
