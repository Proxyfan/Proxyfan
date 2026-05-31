using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="ProtobufDescriptorIndex" /> covering message/enum/method lookup
///     and nested-type indexing.
/// </summary>
public sealed class ProtobufDescriptorIndexTests
{
    /// <summary>
    ///     Top-level messages are reachable via their fully qualified name.
    /// </summary>
    [Test]
    public async Task TryResolveMessage_TopLevelMessage_Returns()
    {
        var index = BuildIndexWithSingleMessage(".demo.Person", "Person");

        var resolved = index.TryResolveMessage(".demo.Person");

        await Assert.That(resolved).IsNotNull();
        await Assert.That(resolved!.Name).IsEqualTo("Person");
    }

    /// <summary>
    ///     Unknown message names resolve to null.
    /// </summary>
    [Test]
    public async Task TryResolveMessage_Unknown_ReturnsNull()
    {
        var index = BuildIndexWithSingleMessage(".demo.Person", "Person");

        var resolved = index.TryResolveMessage(".demo.Missing");

        await Assert.That(resolved).IsNull();
    }

    /// <summary>
    ///     Nested messages are reachable via their parent-qualified name.
    /// </summary>
    [Test]
    public async Task TryResolveMessage_NestedMessage_Returns()
    {
        var nested = BuildEmptyMessage(".demo.Person.Address", "Address");
        var outer = new ProtobufMessageDescriptor
        {
            Fields = new List<ProtobufFieldDescriptor>(),
            FullName = ".demo.Person",
            Name = "Person",
            NestedEnums = new List<ProtobufEnumDescriptor>(),
            NestedMessages = new List<ProtobufMessageDescriptor> { nested },
        };
        var index = BuildIndex(messages: new List<ProtobufMessageDescriptor> { outer });

        var resolved = index.TryResolveMessage(".demo.Person.Address");

        await Assert.That(resolved).IsNotNull();
        await Assert.That(resolved!.Name).IsEqualTo("Address");
    }

    /// <summary>
    ///     Enums declared at file scope resolve via the index.
    /// </summary>
    [Test]
    public async Task TryResolveEnum_TopLevelEnum_Returns()
    {
        var enumDescriptor = new ProtobufEnumDescriptor
        {
            FullName = ".demo.Color",
            Name = "Color",
            Values = new List<ProtobufEnumValueDescriptor>(),
        };
        var index = BuildIndex(enums: new List<ProtobufEnumDescriptor> { enumDescriptor });

        var resolved = index.TryResolveEnum(".demo.Color");

        await Assert.That(resolved).IsNotNull();
    }

    /// <summary>
    ///     Unknown enum names resolve to null.
    /// </summary>
    [Test]
    public async Task TryResolveEnum_Unknown_ReturnsNull()
    {
        var index = BuildIndex();

        var resolved = index.TryResolveEnum(".demo.Missing");

        await Assert.That(resolved).IsNull();
    }

    /// <summary>
    ///     Methods declared on services are reachable via their gRPC path.
    /// </summary>
    [Test]
    public async Task TryResolveMethod_KnownPath_ReturnsMethod()
    {
        var method = new ProtobufMethodDescriptor
        {
            FullPath = "/demo.Greeter/SayHello",
            InputType = ".demo.HelloRequest",
            IsClientStreaming = false,
            IsServerStreaming = false,
            Name = "SayHello",
            OutputType = ".demo.HelloReply",
        };
        var service = new ProtobufServiceDescriptor
        {
            FullName = ".demo.Greeter",
            Methods = new List<ProtobufMethodDescriptor> { method },
            Name = "Greeter",
        };
        var index = BuildIndex(services: new List<ProtobufServiceDescriptor> { service });

        var resolved = index.TryResolveMethod("/demo.Greeter/SayHello");

        await Assert.That(resolved).IsNotNull();
        await Assert.That(resolved!.InputType).IsEqualTo(".demo.HelloRequest");
    }

    /// <summary>
    ///     Unknown method paths resolve to null.
    /// </summary>
    [Test]
    public async Task TryResolveMethod_UnknownPath_ReturnsNull()
    {
        var index = BuildIndex();

        var resolved = index.TryResolveMethod("/foo.Bar/Baz");

        await Assert.That(resolved).IsNull();
    }

    /// <summary>
    ///     The Files property surfaces every supplied file.
    /// </summary>
    [Test]
    public async Task Files_AfterConstruction_EqualsConstructorInput()
    {
        var files = new List<ProtobufFileDescriptor> { BuildEmptyFile("a.proto"), BuildEmptyFile("b.proto") };
        var index = new ProtobufDescriptorIndex(files);

        await Assert.That(index.Files.Count).IsEqualTo(2);
    }

    private static ProtobufMessageDescriptor BuildEmptyMessage(string fullName, string name)
    {
        var descriptor = new ProtobufMessageDescriptor
        {
            Fields = new List<ProtobufFieldDescriptor>(),
            FullName = fullName,
            Name = name,
            NestedEnums = new List<ProtobufEnumDescriptor>(),
            NestedMessages = new List<ProtobufMessageDescriptor>(),
        };
        return descriptor;
    }

    private static ProtobufFileDescriptor BuildEmptyFile(string name)
    {
        var file = new ProtobufFileDescriptor
        {
            Enums = new List<ProtobufEnumDescriptor>(),
            Messages = new List<ProtobufMessageDescriptor>(),
            Name = name,
            Package = string.Empty,
            Services = new List<ProtobufServiceDescriptor>(),
        };
        return file;
    }

    private static ProtobufDescriptorIndex BuildIndex(
        IReadOnlyList<ProtobufMessageDescriptor>? messages = null,
        IReadOnlyList<ProtobufEnumDescriptor>? enums = null,
        IReadOnlyList<ProtobufServiceDescriptor>? services = null)
    {
        var file = new ProtobufFileDescriptor
        {
            Enums = enums ?? Array.Empty<ProtobufEnumDescriptor>(),
            Messages = messages ?? Array.Empty<ProtobufMessageDescriptor>(),
            Name = "test.proto",
            Package = "demo",
            Services = services ?? Array.Empty<ProtobufServiceDescriptor>(),
        };
        var index = new ProtobufDescriptorIndex(new List<ProtobufFileDescriptor> { file });
        return index;
    }

    private static ProtobufDescriptorIndex BuildIndexWithSingleMessage(string fullName, string name)
    {
        var message = BuildEmptyMessage(fullName, name);
        return BuildIndex(messages: new List<ProtobufMessageDescriptor> { message });
    }
}
