using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Schema-aware pretty printer for protobuf payloads. Given a payload, the message
///     descriptor it should conform to, and a <see cref="ProtobufDescriptorIndex" /> for
///     resolving nested message and enum references, renders the payload with field names,
///     declared types, recursive expansion of nested messages, and enum value labels.
///     Falls back gracefully to the schema-less <see cref="ProtobufPrettyPrinter" /> rendering
///     when the payload is malformed; unknown field numbers are rendered as
///     <c>(unknown field N)</c>.
/// </summary>
public static class ProtobufSchemaAwarePrettyPrinter
{
    /// <summary>
    ///     Returns a pretty-printed multi-line rendering of the supplied payload interpreted
    ///     against the supplied descriptor.
    /// </summary>
    /// <param name="payload">The encoded protobuf bytes.</param>
    /// <param name="descriptor">The message descriptor to use for field-name lookup.</param>
    /// <param name="index">The descriptor index for resolving nested types.</param>
    /// <returns>The human-readable rendering.</returns>
    public static string PrettyPrint(ReadOnlyMemory<byte> payload, ProtobufMessageDescriptor descriptor, ProtobufDescriptorIndex index)
    {
        if (payload.IsEmpty)
        {
            return string.Empty;
        }

        IReadOnlyList<ProtobufField> fields;
        try
        {
            fields = ProtobufDecoder.Decode(payload);
        }
        catch (InvalidDataException)
        {
            return ProtobufPrettyPrinter.PrettyPrint(payload);
        }

        var builder = new StringBuilder();
        var context = new ProtobufSchemaAwarePrettyPrintContext
        {
            Builder = builder,
            Index = index,
        };
        WriteFields(context, fields, descriptor, indentLevel: 0);
        return builder.ToString().TrimEnd();
    }

    private static void AppendIndent(StringBuilder builder, int indentLevel)
    {
        for (var index = 0; index < indentLevel; index++)
        {
            builder.Append("  ");
        }
    }

    private static void AppendMalformedMessageBytesField(StringBuilder builder, string fieldName, byte[] bytes, int indentLevel)
    {
        AppendIndent(builder, indentLevel);
        builder.Append(fieldName);
        builder.Append(" (malformed message, ");
        builder.Append(bytes.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(bytes.Length == 1 ? " byte): 0x" : " bytes): 0x");
        for (var byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
        {
            builder.Append(bytes[byteIndex].ToString("x2", CultureInfo.InvariantCulture));
        }

        builder.Append('\n');
    }

    private static void AppendRawBytesField(StringBuilder builder, string fieldName, byte[] bytes, int indentLevel)
    {
        AppendIndent(builder, indentLevel);
        builder.Append(fieldName);
        builder.Append(" (bytes, ");
        builder.Append(bytes.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append("): 0x");
        for (var byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
        {
            builder.Append(bytes[byteIndex].ToString("x2", CultureInfo.InvariantCulture));
        }

        builder.Append('\n');
    }

    private static List<ProtobufField>? DecodePackedElements(byte[] bytes, ProtobufWireType packedWireType, int fieldNumber)
    {
        return packedWireType switch
        {
            ProtobufWireType.Varint => DecodePackedVarints(bytes, fieldNumber),
            ProtobufWireType.I32 => DecodePackedFixed32(bytes, fieldNumber),
            ProtobufWireType.I64 => DecodePackedFixed64(bytes, fieldNumber),
            _ => null,
        };
    }

    private static List<ProtobufField>? DecodePackedFixed32(byte[] bytes, int fieldNumber)
    {
        if (bytes.Length % 4 != 0)
        {
            return null;
        }

        var elements = new List<ProtobufField>(bytes.Length / 4);
        for (var offset = 0; offset < bytes.Length; offset += 4)
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4));
            var element = new ProtobufField(fieldNumber, ProtobufWireType.I32, value);
            elements.Add(element);
        }

        return elements;
    }

    private static List<ProtobufField>? DecodePackedFixed64(byte[] bytes, int fieldNumber)
    {
        if (bytes.Length % 8 != 0)
        {
            return null;
        }

        var elements = new List<ProtobufField>(bytes.Length / 8);
        for (var offset = 0; offset < bytes.Length; offset += 8)
        {
            var value = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset, 8));
            var element = new ProtobufField(fieldNumber, ProtobufWireType.I64, value);
            elements.Add(element);
        }

        return elements;
    }

    private static List<ProtobufField>? DecodePackedVarints(byte[] bytes, int fieldNumber)
    {
        var elements = new List<ProtobufField>();
        var offset = 0;
        while (offset < bytes.Length)
        {
            var read = ReadPackedVarint(bytes, offset);
            if (read is null)
            {
                return null;
            }

            var element = new ProtobufField(fieldNumber, ProtobufWireType.Varint, read.Value.Value);
            elements.Add(element);
            offset = read.Value.NextOffset;
        }

        return elements;
    }

    private static ProtobufFieldDescriptor? FindFieldDescriptor(ProtobufMessageDescriptor messageDescriptor, int fieldNumber)
    {
        for (var index = 0; index < messageDescriptor.Fields.Count; index++)
        {
            var fieldDescriptor = messageDescriptor.Fields[index];
            if (fieldDescriptor.Number == fieldNumber)
            {
                return fieldDescriptor;
            }
        }

        return null;
    }

    private static string FormatEnumValue(ulong rawNumber, ProtobufFieldDescriptor fieldDescriptor, ProtobufDescriptorIndex index)
    {
        var numericText = unchecked((int)rawNumber).ToString(CultureInfo.InvariantCulture);
        if (fieldDescriptor.TypeName is null)
        {
            return numericText;
        }

        var enumDescriptor = index.TryResolveEnum(fieldDescriptor.TypeName);
        if (enumDescriptor is null)
        {
            return numericText;
        }

        for (var valueIndex = 0; valueIndex < enumDescriptor.Values.Count; valueIndex++)
        {
            var enumValue = enumDescriptor.Values[valueIndex];
            if (enumValue.Number == unchecked((int)rawNumber))
            {
                return enumValue.Name + " (" + numericText + ")";
            }
        }

        return numericText;
    }

    private static string FormatFixed32Value(uint raw32, ProtobufFieldDescriptor fieldDescriptor)
    {
        if (fieldDescriptor.Kind == ProtobufFieldKind.TypeFloat)
        {
            var bytes = BitConverter.GetBytes(raw32);
            var value = BitConverter.ToSingle(bytes, 0);
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        if (fieldDescriptor.Kind == ProtobufFieldKind.TypeSignedFixed32)
        {
            return unchecked((int)raw32).ToString(CultureInfo.InvariantCulture);
        }

        return raw32.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatI64Value(ulong raw64, ProtobufFieldDescriptor fieldDescriptor)
    {
        if (fieldDescriptor.Kind == ProtobufFieldKind.TypeDouble)
        {
            var doubleValue = BitConverter.Int64BitsToDouble(unchecked((long)raw64));
            return doubleValue.ToString("R", CultureInfo.InvariantCulture);
        }

        if (fieldDescriptor.Kind == ProtobufFieldKind.TypeSignedFixed64)
        {
            return unchecked((long)raw64).ToString(CultureInfo.InvariantCulture);
        }

        return raw64.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatScalarValue(ProtobufField field, ProtobufFieldDescriptor fieldDescriptor, ProtobufDescriptorIndex index)
    {
        if (fieldDescriptor.Kind == ProtobufFieldKind.TypeBool && field.Value is ulong boolValue)
        {
            return boolValue == 0 ? "false" : "true";
        }

        if (fieldDescriptor.Kind == ProtobufFieldKind.TypeEnum && field.Value is ulong enumValue)
        {
            return FormatEnumValue(enumValue, fieldDescriptor, index);
        }

        if (fieldDescriptor.Kind is ProtobufFieldKind.TypeSignedInt32 or ProtobufFieldKind.TypeSignedInt64 && field.Value is ulong signedRaw)
        {
            var decoded = (long)(signedRaw >> 1) ^ -(long)(signedRaw & 1);
            return decoded.ToString(CultureInfo.InvariantCulture);
        }

        if (fieldDescriptor.Kind is ProtobufFieldKind.TypeInt32 or ProtobufFieldKind.TypeInt64 && field.Value is ulong intRaw)
        {
            return unchecked((long)intRaw).ToString(CultureInfo.InvariantCulture);
        }

        if (field.Value is ulong rawVarint)
        {
            return rawVarint.ToString(CultureInfo.InvariantCulture);
        }

        if (field.Value is uint raw32)
        {
            return FormatFixed32Value(raw32, fieldDescriptor);
        }

        return field.Value.ToString() ?? string.Empty;
    }

    private static ProtobufWireType? GetPackedWireType(ProtobufFieldKind kind)
    {
        if (HasPackedVarintKind(kind))
        {
            return ProtobufWireType.Varint;
        }

        if (HasPackedFixed32Kind(kind))
        {
            return ProtobufWireType.I32;
        }

        if (HasPackedFixed64Kind(kind))
        {
            return ProtobufWireType.I64;
        }

        return null;
    }

    private static bool HasPackedFixed32Kind(ProtobufFieldKind kind)
    {
        return kind is ProtobufFieldKind.TypeFixed32 or ProtobufFieldKind.TypeSignedFixed32 or ProtobufFieldKind.TypeFloat;
    }

    private static bool HasPackedFixed64Kind(ProtobufFieldKind kind)
    {
        return kind is ProtobufFieldKind.TypeFixed64 or ProtobufFieldKind.TypeSignedFixed64 or ProtobufFieldKind.TypeDouble;
    }

    private static bool HasPackedVarintKind(ProtobufFieldKind kind)
    {
        return kind is ProtobufFieldKind.TypeInt32 or ProtobufFieldKind.TypeInt64
            or ProtobufFieldKind.TypeUInt32 or ProtobufFieldKind.TypeUInt64
            or ProtobufFieldKind.TypeSignedInt32 or ProtobufFieldKind.TypeSignedInt64
            or ProtobufFieldKind.TypeBool or ProtobufFieldKind.TypeEnum;
    }

    private static ProtobufPackedVarintRead? ReadPackedVarint(byte[] bytes, int offset)
    {
        ulong value = 0;
        var shift = 0;
        while (true)
        {
            if (offset >= bytes.Length)
            {
                return null;
            }

            var current = bytes[offset];
            offset++;
            if (shift == 63 && (current & 0x7E) != 0)
            {
                return null;
            }

            value |= (ulong)(current & 0x7F) << shift;
            if ((current & 0x80) == 0)
            {
                var read = new ProtobufPackedVarintRead
                {
                    NextOffset = offset,
                    Value = value,
                };
                return read;
            }

            shift += 7;
            if (shift >= 64)
            {
                return null;
            }
        }
    }

    private static IReadOnlyList<ProtobufField>? TryDecodeNested(byte[] bytes)
    {
        try
        {
            return ProtobufDecoder.Decode(bytes);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static void WriteFields(ProtobufSchemaAwarePrettyPrintContext context, IReadOnlyList<ProtobufField> fields, ProtobufMessageDescriptor descriptor, int indentLevel)
    {
        for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            var field = fields[fieldIndex];
            var fieldDescriptor = FindFieldDescriptor(descriptor, field.FieldNumber);
            if (fieldDescriptor is null)
            {
                WriteUnknownField(context.Builder, field, indentLevel);
                continue;
            }

            WriteKnownField(context, field, fieldDescriptor, indentLevel);
        }
    }

    private static void WriteKnownField(ProtobufSchemaAwarePrettyPrintContext context, ProtobufField field, ProtobufFieldDescriptor fieldDescriptor, int indentLevel)
    {
        if (field.WireType == ProtobufWireType.LengthDelimited)
        {
            WriteLengthDelimitedField(context, field, fieldDescriptor, indentLevel);
            return;
        }

        AppendIndent(context.Builder, indentLevel);
        context.Builder.Append(fieldDescriptor.Name);
        context.Builder.Append(": ");

        if (field.WireType == ProtobufWireType.I64 && field.Value is ulong raw64)
        {
            context.Builder.Append(FormatI64Value(raw64, fieldDescriptor));
            context.Builder.Append('\n');
            return;
        }

        context.Builder.Append(FormatScalarValue(field, fieldDescriptor, context.Index));
        context.Builder.Append('\n');
    }

    private static void WriteLengthDelimitedField(ProtobufSchemaAwarePrettyPrintContext context, ProtobufField field, ProtobufFieldDescriptor fieldDescriptor, int indentLevel)
    {
        if (field.Value is not byte[] bytes)
        {
            return;
        }

        if (fieldDescriptor.Kind == ProtobufFieldKind.TypeString)
        {
            AppendIndent(context.Builder, indentLevel);
            context.Builder.Append(fieldDescriptor.Name);
            context.Builder.Append(": \"");
            context.Builder.Append(Encoding.UTF8.GetString(bytes));
            context.Builder.Append("\"\n");
            return;
        }

        if (fieldDescriptor.Label == ProtobufFieldLabel.Repeated)
        {
            var packedWireType = GetPackedWireType(fieldDescriptor.Kind);
            if (packedWireType is not null)
            {
                var elements = DecodePackedElements(bytes, packedWireType.Value, fieldDescriptor.Number);
                if (elements is not null)
                {
                    WritePackedScalarElements(context, elements, fieldDescriptor, indentLevel);
                    return;
                }
            }
        }

        if (fieldDescriptor.Kind == ProtobufFieldKind.TypeMessage && fieldDescriptor.TypeName is not null)
        {
            var nestedDescriptor = context.Index.TryResolveMessage(fieldDescriptor.TypeName);
            if (nestedDescriptor is not null)
            {
                var nestedFields = TryDecodeNested(bytes);
                if (nestedFields is null)
                {
                    AppendMalformedMessageBytesField(context.Builder, fieldDescriptor.Name, bytes, indentLevel);
                    return;
                }

                AppendIndent(context.Builder, indentLevel);
                context.Builder.Append(fieldDescriptor.Name);
                context.Builder.Append(" {\n");
                WriteFields(context, nestedFields, nestedDescriptor, indentLevel + 1);
                AppendIndent(context.Builder, indentLevel);
                context.Builder.Append("}\n");
                return;
            }
        }

        AppendRawBytesField(context.Builder, fieldDescriptor.Name, bytes, indentLevel);
    }

    private static void WritePackedScalarElements(ProtobufSchemaAwarePrettyPrintContext context, List<ProtobufField> elements, ProtobufFieldDescriptor fieldDescriptor, int indentLevel)
    {
        for (var index = 0; index < elements.Count; index++)
        {
            var element = elements[index];
            AppendIndent(context.Builder, indentLevel);
            context.Builder.Append(fieldDescriptor.Name);
            context.Builder.Append(": ");
            if (element.WireType == ProtobufWireType.I64 && element.Value is ulong raw64)
            {
                context.Builder.Append(FormatI64Value(raw64, fieldDescriptor));
            }
            else
            {
                context.Builder.Append(FormatScalarValue(element, fieldDescriptor, context.Index));
            }

            context.Builder.Append('\n');
        }
    }

    private static void WriteUnknownField(StringBuilder builder, ProtobufField field, int indentLevel)
    {
        AppendIndent(builder, indentLevel);
        builder.Append("(unknown field ");
        builder.Append(field.FieldNumber.ToString(CultureInfo.InvariantCulture));
        builder.Append("): ");
        if (field.Value is byte[] bytes)
        {
            for (var index = 0; index < bytes.Length; index++)
            {
                builder.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
            }
        }
        else
        {
            builder.Append(field.Value.ToString() ?? string.Empty);
        }

        builder.Append('\n');
    }
}
