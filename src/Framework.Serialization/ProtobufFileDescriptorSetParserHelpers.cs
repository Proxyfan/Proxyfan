using System.Collections.Generic;
using System.Text;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Internal helper used by <see cref="ProtobufFileDescriptorSetParser" /> to keep
///     individual parse methods under the analyzer-enforced complexity limits. Provides
///     focused single-field readers for varint, string, and length-delimited submessage
///     fields keyed by field number.
/// </summary>
public static class ProtobufFileDescriptorSetParserHelpers
{
    /// <summary>
    ///     Builds a fully qualified protobuf name by joining a parent full name
    ///     (with leading dot) and a local name.
    /// </summary>
    /// <param name="parentFullName">The parent's fully qualified name (may be empty).</param>
    /// <param name="localName">The local name to append.</param>
    /// <returns>The combined fully qualified name.</returns>
    public static string BuildFullName(string parentFullName, string localName)
    {
        if (string.IsNullOrEmpty(parentFullName))
        {
            return "." + localName;
        }

        return parentFullName + "." + localName;
    }

    /// <summary>
    ///     Reads the first varint field with the supplied field number and interprets it
    ///     as a boolean.
    /// </summary>
    /// <param name="fields">The decoded fields to scan.</param>
    /// <param name="fieldNumber">The protobuf field number to match.</param>
    /// <param name="value">When found, receives the parsed boolean.</param>
    /// <returns><see langword="true" /> when a matching field was found.</returns>
    public static bool HasBoolField(IReadOnlyList<ProtobufField> fields, int fieldNumber, out bool value)
    {
        value = false;
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == fieldNumber && field.Value is ulong rawNumber)
            {
                value = rawNumber != 0;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Reads the first varint field with the supplied field number and interprets it
    ///     as an <see cref="int" />.
    /// </summary>
    /// <param name="fields">The decoded fields to scan.</param>
    /// <param name="fieldNumber">The protobuf field number to match.</param>
    /// <param name="value">When found, receives the parsed integer.</param>
    /// <returns><see langword="true" /> when a matching field was found.</returns>
    public static bool HasInt32Field(IReadOnlyList<ProtobufField> fields, int fieldNumber, out int value)
    {
        value = 0;
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == fieldNumber && field.Value is ulong rawNumber)
            {
                value = unchecked((int)rawNumber);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Reads the first length-delimited field with the supplied field number and
    ///     interprets its bytes as UTF-8. Returns an empty string when no matching field
    ///     is present so callers don't need to null-check required scalar names.
    /// </summary>
    /// <param name="fields">The decoded fields to scan.</param>
    /// <param name="fieldNumber">The protobuf field number to match.</param>
    /// <returns>The decoded UTF-8 string, or an empty string when absent.</returns>
    public static string ReadStringField(IReadOnlyList<ProtobufField> fields, int fieldNumber)
    {
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == fieldNumber && field.Value is byte[] bytes)
            {
                return Encoding.UTF8.GetString(bytes);
            }
        }

        return string.Empty;
    }

    /// <summary>
    ///     Reads the first length-delimited field with the supplied field number and
    ///     interprets its bytes as UTF-8, returning <see langword="null" /> when the field
    ///     is absent (used for optional string fields that distinguish missing from empty).
    /// </summary>
    /// <param name="fields">The decoded fields to scan.</param>
    /// <param name="fieldNumber">The protobuf field number to match.</param>
    /// <returns>The decoded UTF-8 string, or <see langword="null" /> when absent.</returns>
    public static string? TryReadStringField(IReadOnlyList<ProtobufField> fields, int fieldNumber)
    {
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == fieldNumber && field.Value is byte[] bytes)
            {
                return Encoding.UTF8.GetString(bytes);
            }
        }

        return null;
    }
}
