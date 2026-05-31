using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="ProtobufFileDescriptorSetParser" />. Builds tiny
///     FileDescriptorSet payloads with <see cref="ProtobufWireWriter" /> and verifies the
///     parser reconstructs the equivalent descriptor model.
/// </summary>
public sealed class ProtobufFileDescriptorSetParserTests
{
    /// <summary>
    ///     An empty FileDescriptorSet parses to an empty file list.
    /// </summary>
    [Test]
    public async Task Parse_EmptyPayload_ReturnsEmptyFileList()
    {
        var files = ProtobufFileDescriptorSetParser.Parse(ReadOnlyMemory<byte>.Empty);

        await Assert.That(files.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     A file with name + package + no messages parses with both surface attributes
    ///     populated.
    /// </summary>
    [Test]
    public async Task Parse_FileWithNameAndPackage_RoundTripsBothFields()
    {
        var fileBytes = new ProtobufWireWriter()
            .WriteStringField(1, "greeter.proto")
            .WriteStringField(2, "foo.bar")
            .ToArray();
        var setBytes = new ProtobufWireWriter()
            .WriteBytesField(1, fileBytes)
            .ToArray();

        var files = ProtobufFileDescriptorSetParser.Parse(setBytes);

        await Assert.That(files.Count).IsEqualTo(1);
        await Assert.That(files[0].Name).IsEqualTo("greeter.proto");
        await Assert.That(files[0].Package).IsEqualTo("foo.bar");
    }

    /// <summary>
    ///     A top-level message with scalar fields parses with each field's name, number,
    ///     label, and kind populated.
    /// </summary>
    [Test]
    public async Task Parse_MessageWithScalarFields_PopulatesFieldDescriptors()
    {
        var nameFieldBytes = new ProtobufWireWriter()
            .WriteStringField(1, "name")
            .WriteVarintField(3, 1u)
            .WriteVarintField(4, (ulong)ProtobufFieldLabel.Optional)
            .WriteVarintField(5, (ulong)ProtobufFieldKind.TypeString)
            .ToArray();
        var ageFieldBytes = new ProtobufWireWriter()
            .WriteStringField(1, "age")
            .WriteVarintField(3, 2u)
            .WriteVarintField(4, (ulong)ProtobufFieldLabel.Optional)
            .WriteVarintField(5, (ulong)ProtobufFieldKind.TypeInt32)
            .ToArray();
        var messageBytes = new ProtobufWireWriter()
            .WriteStringField(1, "Person")
            .WriteBytesField(2, nameFieldBytes)
            .WriteBytesField(2, ageFieldBytes)
            .ToArray();
        var fileBytes = new ProtobufWireWriter()
            .WriteStringField(1, "person.proto")
            .WriteStringField(2, "demo")
            .WriteBytesField(4, messageBytes)
            .ToArray();
        var setBytes = new ProtobufWireWriter()
            .WriteBytesField(1, fileBytes)
            .ToArray();

        var files = ProtobufFileDescriptorSetParser.Parse(setBytes);
        var message = files[0].Messages[0];

        await Assert.That(message.FullName).IsEqualTo(".demo.Person");
        await Assert.That(message.Fields.Count).IsEqualTo(2);
        await Assert.That(message.Fields[0].Name).IsEqualTo("name");
        await Assert.That(message.Fields[0].Number).IsEqualTo(1);
        await Assert.That(message.Fields[0].Kind).IsEqualTo(ProtobufFieldKind.TypeString);
        await Assert.That(message.Fields[1].Name).IsEqualTo("age");
        await Assert.That(message.Fields[1].Kind).IsEqualTo(ProtobufFieldKind.TypeInt32);
    }

    /// <summary>
    ///     A nested message inside a parent message gets a fully qualified name that
    ///     includes both the file's package and the parent message's name.
    /// </summary>
    [Test]
    public async Task Parse_NestedMessage_HasParentQualifiedFullName()
    {
        var innerMessageBytes = new ProtobufWireWriter()
            .WriteStringField(1, "Address")
            .ToArray();
        var outerMessageBytes = new ProtobufWireWriter()
            .WriteStringField(1, "Person")
            .WriteBytesField(3, innerMessageBytes)
            .ToArray();
        var fileBytes = new ProtobufWireWriter()
            .WriteStringField(1, "person.proto")
            .WriteStringField(2, "demo")
            .WriteBytesField(4, outerMessageBytes)
            .ToArray();
        var setBytes = new ProtobufWireWriter()
            .WriteBytesField(1, fileBytes)
            .ToArray();

        var files = ProtobufFileDescriptorSetParser.Parse(setBytes);
        var nested = files[0].Messages[0].NestedMessages[0];

        await Assert.That(nested.FullName).IsEqualTo(".demo.Person.Address");
    }

    /// <summary>
    ///     A top-level enum and its values round-trip through the parser.
    /// </summary>
    [Test]
    public async Task Parse_TopLevelEnum_PopulatesValues()
    {
        var redValueBytes = new ProtobufWireWriter()
            .WriteStringField(1, "RED")
            .WriteVarintField(2, 0u)
            .ToArray();
        var greenValueBytes = new ProtobufWireWriter()
            .WriteStringField(1, "GREEN")
            .WriteVarintField(2, 1u)
            .ToArray();
        var enumBytes = new ProtobufWireWriter()
            .WriteStringField(1, "Color")
            .WriteBytesField(2, redValueBytes)
            .WriteBytesField(2, greenValueBytes)
            .ToArray();
        var fileBytes = new ProtobufWireWriter()
            .WriteStringField(1, "color.proto")
            .WriteStringField(2, "demo")
            .WriteBytesField(5, enumBytes)
            .ToArray();
        var setBytes = new ProtobufWireWriter()
            .WriteBytesField(1, fileBytes)
            .ToArray();

        var files = ProtobufFileDescriptorSetParser.Parse(setBytes);
        var enumDescriptor = files[0].Enums[0];

        await Assert.That(enumDescriptor.FullName).IsEqualTo(".demo.Color");
        await Assert.That(enumDescriptor.Values.Count).IsEqualTo(2);
        await Assert.That(enumDescriptor.Values[0].Name).IsEqualTo("RED");
        await Assert.That(enumDescriptor.Values[0].Number).IsEqualTo(0);
        await Assert.That(enumDescriptor.Values[1].Name).IsEqualTo("GREEN");
        await Assert.That(enumDescriptor.Values[1].Number).IsEqualTo(1);
    }

    /// <summary>
    ///     A service with one unary RPC method produces a method descriptor whose FullPath
    ///     equals the gRPC wire path <c>/package.Service/Method</c>.
    /// </summary>
    [Test]
    public async Task Parse_ServiceWithUnaryMethod_BuildsGrpcPath()
    {
        var methodBytes = new ProtobufWireWriter()
            .WriteStringField(1, "SayHello")
            .WriteStringField(2, ".demo.HelloRequest")
            .WriteStringField(3, ".demo.HelloReply")
            .ToArray();
        var serviceBytes = new ProtobufWireWriter()
            .WriteStringField(1, "Greeter")
            .WriteBytesField(2, methodBytes)
            .ToArray();
        var fileBytes = new ProtobufWireWriter()
            .WriteStringField(1, "greeter.proto")
            .WriteStringField(2, "demo")
            .WriteBytesField(6, serviceBytes)
            .ToArray();
        var setBytes = new ProtobufWireWriter()
            .WriteBytesField(1, fileBytes)
            .ToArray();

        var files = ProtobufFileDescriptorSetParser.Parse(setBytes);
        var method = files[0].Services[0].Methods[0];

        await Assert.That(files[0].Services[0].FullName).IsEqualTo(".demo.Greeter");
        await Assert.That(method.Name).IsEqualTo("SayHello");
        await Assert.That(method.FullPath).IsEqualTo("/demo.Greeter/SayHello");
        await Assert.That(method.InputType).IsEqualTo(".demo.HelloRequest");
        await Assert.That(method.OutputType).IsEqualTo(".demo.HelloReply");
        await Assert.That(method.IsClientStreaming).IsFalse();
        await Assert.That(method.IsServerStreaming).IsFalse();
    }

    /// <summary>
    ///     Streaming flags on a method round-trip through the parser.
    /// </summary>
    [Test]
    public async Task Parse_BidirectionalStreamingMethod_SetsBothFlags()
    {
        var methodBytes = new ProtobufWireWriter()
            .WriteStringField(1, "Chat")
            .WriteStringField(2, ".demo.ChatMessage")
            .WriteStringField(3, ".demo.ChatMessage")
            .WriteBoolField(5, true)
            .WriteBoolField(6, true)
            .ToArray();
        var serviceBytes = new ProtobufWireWriter()
            .WriteStringField(1, "Chatter")
            .WriteBytesField(2, methodBytes)
            .ToArray();
        var fileBytes = new ProtobufWireWriter()
            .WriteStringField(1, "chat.proto")
            .WriteBytesField(6, serviceBytes)
            .ToArray();
        var setBytes = new ProtobufWireWriter()
            .WriteBytesField(1, fileBytes)
            .ToArray();

        var files = ProtobufFileDescriptorSetParser.Parse(setBytes);
        var method = files[0].Services[0].Methods[0];

        await Assert.That(method.FullPath).IsEqualTo("/Chatter/Chat");
        await Assert.That(method.IsClientStreaming).IsTrue();
        await Assert.That(method.IsServerStreaming).IsTrue();
    }

    /// <summary>
    ///     A message-typed field carries its referenced type name.
    /// </summary>
    [Test]
    public async Task Parse_MessageTypedField_PreservesTypeName()
    {
        var fieldBytes = new ProtobufWireWriter()
            .WriteStringField(1, "address")
            .WriteVarintField(3, 1u)
            .WriteVarintField(4, (ulong)ProtobufFieldLabel.Optional)
            .WriteVarintField(5, (ulong)ProtobufFieldKind.TypeMessage)
            .WriteStringField(6, ".demo.Address")
            .ToArray();
        var messageBytes = new ProtobufWireWriter()
            .WriteStringField(1, "Person")
            .WriteBytesField(2, fieldBytes)
            .ToArray();
        var fileBytes = new ProtobufWireWriter()
            .WriteStringField(1, "person.proto")
            .WriteStringField(2, "demo")
            .WriteBytesField(4, messageBytes)
            .ToArray();
        var setBytes = new ProtobufWireWriter()
            .WriteBytesField(1, fileBytes)
            .ToArray();

        var files = ProtobufFileDescriptorSetParser.Parse(setBytes);
        var field = files[0].Messages[0].Fields[0];

        await Assert.That(field.Kind).IsEqualTo(ProtobufFieldKind.TypeMessage);
        await Assert.That(field.TypeName).IsEqualTo(".demo.Address");
    }
}
