namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Mirrors <c>google.protobuf.FieldDescriptorProto.Label</c> from <c>descriptor.proto</c>.
///     Identifies the multiplicity of a protobuf field.
/// </summary>
public enum ProtobufFieldLabel
{
    /// <summary>
    ///     <c>LABEL_OPTIONAL</c> (1): zero or one occurrences (proto3 default).
    /// </summary>
    Optional = 1,

    /// <summary>
    ///     <c>LABEL_REQUIRED</c> (2): exactly one occurrence (proto2 only).
    /// </summary>
    Required = 2,

    /// <summary>
    ///     <c>LABEL_REPEATED</c> (3): zero or more occurrences.
    /// </summary>
    Repeated = 3,
}
