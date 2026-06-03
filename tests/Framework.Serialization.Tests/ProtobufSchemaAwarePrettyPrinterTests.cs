using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="ProtobufSchemaAwarePrettyPrinter" /> covering scalar fields,
///     enum lookup, nested messages, unknown fields, and malformed payload fallback.
/// </summary>
public sealed class ProtobufSchemaAwarePrettyPrinterTests
{
    /// <summary>
    ///     A varint integer field renders as <c>name: value</c>.
    /// </summary>
    [Test]
    public async Task PrettyPrint_Int32Field_RendersNameValuePair()
    {
        var descriptor = BuildMessage(".demo.Hello", "Hello", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeInt32, Label = ProtobufFieldLabel.Optional, Name = "id", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var payload = new ProtobufWireWriter().WriteVarintField(1, 42).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("id: 42");
    }

    /// <summary>
    ///     A string field renders the UTF-8 value inside quotes.
    /// </summary>
    [Test]
    public async Task PrettyPrint_StringField_RendersQuotedValue()
    {
        var descriptor = BuildMessage(".demo.Hello", "Hello", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeString, Label = ProtobufFieldLabel.Optional, Name = "greeting", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var payload = new ProtobufWireWriter().WriteStringField(1, "hello world").ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("greeting: \"hello world\"");
    }

    /// <summary>
    ///     A bool field renders as <c>true</c>/<c>false</c>.
    /// </summary>
    [Test]
    public async Task PrettyPrint_BoolField_RendersTrueLiteral()
    {
        var descriptor = BuildMessage(".demo.Flag", "Flag", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeBool, Label = ProtobufFieldLabel.Optional, Name = "ok", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var payload = new ProtobufWireWriter().WriteVarintField(1, 1).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("ok: true");
    }

    /// <summary>
    ///     Enum field values resolve via the descriptor index to <c>NAME (number)</c>.
    /// </summary>
    [Test]
    public async Task PrettyPrint_EnumField_RendersEnumValueName()
    {
        var enumDescriptor = new ProtobufEnumDescriptor
        {
            FullName = ".demo.Color",
            Name = "Color",
            Values = new List<ProtobufEnumValueDescriptor>
            {
                new() { Name = "RED", Number = 0 },
                new() { Name = "GREEN", Number = 1 },
            },
        };
        var descriptor = BuildMessage(".demo.Paint", "Paint", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeEnum, Label = ProtobufFieldLabel.Optional, Name = "color", Number = 1, TypeName = ".demo.Color" },
        });
        var file = new ProtobufFileDescriptor
        {
            Enums = new List<ProtobufEnumDescriptor> { enumDescriptor },
            Messages = new List<ProtobufMessageDescriptor> { descriptor },
            Name = "paint.proto",
            Package = "demo",
            Services = Array.Empty<ProtobufServiceDescriptor>(),
        };
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { file });
        var payload = new ProtobufWireWriter().WriteVarintField(1, 1).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("color: GREEN (1)");
    }

    /// <summary>
    ///     Unknown enum values fall back to the numeric value.
    /// </summary>
    [Test]
    public async Task PrettyPrint_UnresolvedEnumValue_RendersNumberOnly()
    {
        var enumDescriptor = new ProtobufEnumDescriptor
        {
            FullName = ".demo.Color",
            Name = "Color",
            Values = new List<ProtobufEnumValueDescriptor>
            {
                new() { Name = "RED", Number = 0 },
            },
        };
        var descriptor = BuildMessage(".demo.Paint", "Paint", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeEnum, Label = ProtobufFieldLabel.Optional, Name = "color", Number = 1, TypeName = ".demo.Color" },
        });
        var file = new ProtobufFileDescriptor
        {
            Enums = new List<ProtobufEnumDescriptor> { enumDescriptor },
            Messages = new List<ProtobufMessageDescriptor> { descriptor },
            Name = "paint.proto",
            Package = "demo",
            Services = Array.Empty<ProtobufServiceDescriptor>(),
        };
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { file });
        var payload = new ProtobufWireWriter().WriteVarintField(1, 99).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("color: 99");
    }

    /// <summary>
    ///     A nested message field whose bytes are malformed renders as a raw hex dump
    ///     with a malformed-message marker rather than as an empty <c>name { }</c> block.
    /// </summary>
    [Test]
    public async Task PrettyPrint_MalformedNestedMessage_RendersMalformedMarker()
    {
        var innerDescriptor = BuildMessage(".demo.Inner", "Inner", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeString, Label = ProtobufFieldLabel.Optional, Name = "label", Number = 1 },
        });
        var outerDescriptor = BuildMessage(".demo.Outer", "Outer", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeMessage, Label = ProtobufFieldLabel.Optional, Name = "inner", Number = 1, TypeName = ".demo.Inner" },
        });
        var file = new ProtobufFileDescriptor
        {
            Enums = Array.Empty<ProtobufEnumDescriptor>(),
            Messages = new List<ProtobufMessageDescriptor> { innerDescriptor, outerDescriptor },
            Name = "nested.proto",
            Package = "demo",
            Services = Array.Empty<ProtobufServiceDescriptor>(),
        };
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { file });
        var malformedInnerBytes = new byte[] { 0x80, 0x80 };
        var outerBytes = new ProtobufWireWriter().WriteBytesField(1, malformedInnerBytes).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(outerBytes, outerDescriptor, index);

        await Assert.That(rendering).Contains("inner (malformed message, 2 bytes): 0x8080");
        await Assert.That(rendering).DoesNotContain("inner {");
    }

    /// <summary>
    ///     A nested message field renders recursively with the inner field expanded.
    /// </summary>
    [Test]
    public async Task PrettyPrint_NestedMessageField_RendersRecursively()
    {
        var innerDescriptor = BuildMessage(".demo.Inner", "Inner", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeString, Label = ProtobufFieldLabel.Optional, Name = "label", Number = 1 },
        });
        var outerDescriptor = BuildMessage(".demo.Outer", "Outer", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeMessage, Label = ProtobufFieldLabel.Optional, Name = "inner", Number = 1, TypeName = ".demo.Inner" },
        });
        var file = new ProtobufFileDescriptor
        {
            Enums = Array.Empty<ProtobufEnumDescriptor>(),
            Messages = new List<ProtobufMessageDescriptor> { innerDescriptor, outerDescriptor },
            Name = "nested.proto",
            Package = "demo",
            Services = Array.Empty<ProtobufServiceDescriptor>(),
        };
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { file });
        var innerBytes = new ProtobufWireWriter().WriteStringField(1, "hi").ToArray();
        var outerBytes = new ProtobufWireWriter().WriteBytesField(1, innerBytes).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(outerBytes, outerDescriptor, index);

        await Assert.That(rendering).Contains("inner {");
        await Assert.That(rendering).Contains("label: \"hi\"");
        await Assert.That(rendering).Contains("}");
    }

    /// <summary>
    ///     A field whose number is not declared in the descriptor renders as
    ///     <c>(unknown field N)</c>.
    /// </summary>
    [Test]
    public async Task PrettyPrint_UnknownFieldNumber_RendersUnknownLabel()
    {
        var descriptor = BuildMessage(".demo.Hello", "Hello", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeInt32, Label = ProtobufFieldLabel.Optional, Name = "id", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var payload = new ProtobufWireWriter().WriteVarintField(99, 7).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).Contains("(unknown field 99)");
    }

    /// <summary>
    ///     An empty payload returns an empty string.
    /// </summary>
    [Test]
    public async Task PrettyPrint_EmptyPayload_ReturnsEmptyString()
    {
        var descriptor = BuildMessage(".demo.Hello", "Hello", new List<ProtobufFieldDescriptor>());
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(ReadOnlyMemory<byte>.Empty, descriptor, index);

        await Assert.That(rendering).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     A malformed (truncated varint) payload falls back to the schema-less hex rendering.
    /// </summary>
    [Test]
    public async Task PrettyPrint_MalformedPayload_FallsBackToHex()
    {
        var descriptor = BuildMessage(".demo.Hello", "Hello", new List<ProtobufFieldDescriptor>());
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var payload = new byte[] { 0x80 };

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("80");
    }

    /// <summary>
    ///     A bytes field renders as hex with a length annotation.
    /// </summary>
    [Test]
    public async Task PrettyPrint_BytesField_RendersHex()
    {
        var descriptor = BuildMessage(".demo.Data", "Data", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeBytes, Label = ProtobufFieldLabel.Optional, Name = "payload", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var data = new byte[] { 0xDE, 0xAD };
        var payload = new ProtobufWireWriter().WriteBytesField(1, data).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).Contains("payload (bytes, 2)");
        await Assert.That(rendering).Contains("0xdead");
    }

    /// <summary>
    ///     A signed-int32 field uses zig-zag decoding.
    /// </summary>
    [Test]
    public async Task PrettyPrint_SignedInt32_DecodesZigZag()
    {
        var descriptor = BuildMessage(".demo.Hello", "Hello", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeSignedInt32, Label = ProtobufFieldLabel.Optional, Name = "delta", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var payload = new ProtobufWireWriter().WriteVarintField(1, 1).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("delta: -1");
    }

    /// <summary>
    ///     A fixed32 float field renders the IEEE 754 single-precision value.
    /// </summary>
    [Test]
    public async Task PrettyPrint_FloatField_RendersIeee754Value()
    {
        var descriptor = BuildMessage(".demo.Vec", "Vec", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeFloat, Label = ProtobufFieldLabel.Optional, Name = "x", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var bytes = BitConverter.GetBytes(1.5f);
        var raw = BitConverter.ToUInt32(bytes, 0);
        var payload = new ProtobufWireWriter().WriteFixed32Field(1, raw).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("x: 1.5");
    }

    /// <summary>
    ///     A fixed64 double field renders the IEEE 754 double-precision value.
    /// </summary>
    [Test]
    public async Task PrettyPrint_DoubleField_RendersIeee754Value()
    {
        var descriptor = BuildMessage(".demo.Vec", "Vec", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeDouble, Label = ProtobufFieldLabel.Optional, Name = "x", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var raw = unchecked((ulong)BitConverter.DoubleToInt64Bits(2.5));
        var payload = new ProtobufWireWriter().WriteFixed64Field(1, raw).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("x: 2.5");
    }

    /// <summary>
    ///     A packed repeated int32 field decodes the length-delimited payload into
    ///     individual varint elements rendered as a list.
    /// </summary>
    [Test]
    public async Task PrettyPrint_PackedRepeatedInt32_RendersListOfValues()
    {
        var descriptor = BuildMessage(".demo.Numbers", "Numbers", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeInt32, Label = ProtobufFieldLabel.Repeated, Name = "values", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        // Packed payload: three varints 1, 2, 150 (150 encodes as 0x96 0x01).
        var packed = new byte[] { 0x01, 0x02, 0x96, 0x01 };
        var payload = new ProtobufWireWriter().WriteBytesField(1, packed).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("values: [1, 2, 150]");
    }

    /// <summary>
    ///     A packed repeated bool field renders each element as <c>true</c>/<c>false</c>.
    /// </summary>
    [Test]
    public async Task PrettyPrint_PackedRepeatedBool_RendersBooleanLiterals()
    {
        var descriptor = BuildMessage(".demo.Flags", "Flags", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeBool, Label = ProtobufFieldLabel.Repeated, Name = "flags", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var packed = new byte[] { 0x01, 0x00, 0x01 };
        var payload = new ProtobufWireWriter().WriteBytesField(1, packed).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("flags: [true, false, true]");
    }

    /// <summary>
    ///     A packed repeated sint32 field applies zig-zag decoding to each element.
    /// </summary>
    [Test]
    public async Task PrettyPrint_PackedRepeatedSignedInt32_DecodesZigZagPerElement()
    {
        var descriptor = BuildMessage(".demo.Deltas", "Deltas", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeSignedInt32, Label = ProtobufFieldLabel.Repeated, Name = "deltas", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        // Zig-zag: 0 -> 0, -1 -> 1, 1 -> 2.
        var packed = new byte[] { 0x00, 0x01, 0x02 };
        var payload = new ProtobufWireWriter().WriteBytesField(1, packed).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("deltas: [0, -1, 1]");
    }

    /// <summary>
    ///     A packed repeated enum field resolves each numeric element via the descriptor
    ///     index to <c>NAME (number)</c>.
    /// </summary>
    [Test]
    public async Task PrettyPrint_PackedRepeatedEnum_RendersEnumValueNames()
    {
        var enumDescriptor = new ProtobufEnumDescriptor
        {
            FullName = ".demo.Color",
            Name = "Color",
            Values = new List<ProtobufEnumValueDescriptor>
            {
                new() { Name = "RED", Number = 0 },
                new() { Name = "GREEN", Number = 1 },
            },
        };
        var descriptor = BuildMessage(".demo.Palette", "Palette", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeEnum, Label = ProtobufFieldLabel.Repeated, Name = "colors", Number = 1, TypeName = ".demo.Color" },
        });
        var file = new ProtobufFileDescriptor
        {
            Enums = new List<ProtobufEnumDescriptor> { enumDescriptor },
            Messages = new List<ProtobufMessageDescriptor> { descriptor },
            Name = "palette.proto",
            Package = "demo",
            Services = Array.Empty<ProtobufServiceDescriptor>(),
        };
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { file });
        var packed = new byte[] { 0x01, 0x00, 0x01 };
        var payload = new ProtobufWireWriter().WriteBytesField(1, packed).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("colors: [GREEN (1), RED (0), GREEN (1)]");
    }

    /// <summary>
    ///     A packed repeated fixed32 float field decodes each 4-byte IEEE 754 element.
    /// </summary>
    [Test]
    public async Task PrettyPrint_PackedRepeatedFloat_DecodesIeee754Elements()
    {
        var descriptor = BuildMessage(".demo.Vec", "Vec", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeFloat, Label = ProtobufFieldLabel.Repeated, Name = "coords", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var packed = new byte[8];
        BinaryPrimitives.WriteSingleLittleEndian(packed.AsSpan(0, 4), 1.5f);
        BinaryPrimitives.WriteSingleLittleEndian(packed.AsSpan(4, 4), 2.5f);
        var payload = new ProtobufWireWriter().WriteBytesField(1, packed).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("coords: [1.5, 2.5]");
    }

    /// <summary>
    ///     A packed repeated fixed64 double field decodes each 8-byte IEEE 754 element.
    /// </summary>
    [Test]
    public async Task PrettyPrint_PackedRepeatedDouble_DecodesIeee754Elements()
    {
        var descriptor = BuildMessage(".demo.Vec", "Vec", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeDouble, Label = ProtobufFieldLabel.Repeated, Name = "coords", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var packed = new byte[16];
        BinaryPrimitives.WriteDoubleLittleEndian(packed.AsSpan(0, 8), 1.5);
        BinaryPrimitives.WriteDoubleLittleEndian(packed.AsSpan(8, 8), 2.5);
        var payload = new ProtobufWireWriter().WriteBytesField(1, packed).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("coords: [1.5, 2.5]");
    }

    /// <summary>
    ///     A packed repeated payload that is truncated falls back to the raw hex rendering.
    /// </summary>
    [Test]
    public async Task PrettyPrint_PackedRepeatedMalformed_FallsBackToHex()
    {
        var descriptor = BuildMessage(".demo.Numbers", "Numbers", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeInt32, Label = ProtobufFieldLabel.Repeated, Name = "values", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        // Truncated varint (continuation bit set, no follow-up byte).
        var packed = new byte[] { 0x80 };
        var payload = new ProtobufWireWriter().WriteBytesField(1, packed).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).Contains("values (bytes, 1)");
        await Assert.That(rendering).Contains("0x80");
    }

    /// <summary>
    ///     An empty packed repeated payload renders as an empty list.
    /// </summary>
    [Test]
    public async Task PrettyPrint_PackedRepeatedEmpty_RendersEmptyList()
    {
        var descriptor = BuildMessage(".demo.Numbers", "Numbers", new List<ProtobufFieldDescriptor>
        {
            new() { Kind = ProtobufFieldKind.TypeInt32, Label = ProtobufFieldLabel.Repeated, Name = "values", Number = 1 },
        });
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { BuildFileWith(descriptor) });
        var payload = new ProtobufWireWriter().WriteBytesField(1, Array.Empty<byte>()).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, descriptor, index);

        await Assert.That(rendering).IsEqualTo("values: []");
    }

    private static ProtobufMessageDescriptor BuildMessage(string fullName, string name, IReadOnlyList<ProtobufFieldDescriptor> fields)
    {
        var descriptor = new ProtobufMessageDescriptor
        {
            Fields = fields,
            FullName = fullName,
            Name = name,
            NestedEnums = new List<ProtobufEnumDescriptor>(),
            NestedMessages = new List<ProtobufMessageDescriptor>(),
        };
        return descriptor;
    }

    private static ProtobufFileDescriptor BuildFileWith(ProtobufMessageDescriptor descriptor)
    {
        var file = new ProtobufFileDescriptor
        {
            Enums = Array.Empty<ProtobufEnumDescriptor>(),
            Messages = new List<ProtobufMessageDescriptor> { descriptor },
            Name = "test.proto",
            Package = "demo",
            Services = Array.Empty<ProtobufServiceDescriptor>(),
        };
        return file;
    }
}
