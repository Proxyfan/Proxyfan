using System;
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
        var malformedInnerBytes = new byte[] { 0x80 };
        var outerBytes = new ProtobufWireWriter().WriteBytesField(1, malformedInnerBytes).ToArray();

        var rendering = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(outerBytes, outerDescriptor, index);

        await Assert.That(rendering).Contains("inner (malformed message, 1 bytes): 0x80");
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
