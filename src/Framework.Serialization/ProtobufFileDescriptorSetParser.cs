using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Parses a <c>google.protobuf.FileDescriptorSet</c> binary payload (typically produced
///     by <c>protoc --descriptor_set_out=descriptors.pb</c>) into a list of
///     <see cref="ProtobufFileDescriptor" /> records. Implemented on top of the schema-less
///     <see cref="ProtobufDecoder" /> so Proxyfan ships zero protobuf runtime dependencies.
///     <para>
///         Only the subset of <c>descriptor.proto</c> required for inspector decoding is
///         interpreted: file/package/messages/enums/services/methods/fields. Field options,
///         source code info, oneofs, and reserved ranges are ignored without error.
///     </para>
/// </summary>
public static class ProtobufFileDescriptorSetParser
{
    private const int FileDescriptorProtoFieldNumber = 1;

    /// <summary>
    ///     Parses a binary FileDescriptorSet payload.
    /// </summary>
    /// <param name="payload">The descriptor bytes.</param>
    /// <returns>The parsed file descriptors in declaration order.</returns>
    /// <exception cref="System.IO.InvalidDataException">
    ///     Thrown when the payload is malformed and cannot be decoded as a protobuf wire
    ///     message.
    /// </exception>
    public static IReadOnlyList<ProtobufFileDescriptor> Parse(ReadOnlyMemory<byte> payload)
    {
        var topLevelFields = ProtobufDecoder.Decode(payload);
        var files = new List<ProtobufFileDescriptor>();
        for (var index = 0; index < topLevelFields.Count; index++)
        {
            var field = topLevelFields[index];
            if (field.FieldNumber != FileDescriptorProtoFieldNumber)
            {
                continue;
            }

            if (field.Value is byte[] bytes)
            {
                var file = ParseFile(bytes);
                files.Add(file);
            }
        }

        return files;
    }

    private static string BuildMethodPath(string servicePackage, string serviceName, string methodName)
    {
        if (string.IsNullOrEmpty(servicePackage))
        {
            return "/" + serviceName + "/" + methodName;
        }

        return "/" + servicePackage + "." + serviceName + "/" + methodName;
    }

    private static List<ProtobufEnumValueDescriptor> CollectEnumValues(IReadOnlyList<ProtobufField> fields)
    {
        var values = new List<ProtobufEnumValueDescriptor>();
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == 2 && field.Value is byte[] valueBytes)
            {
                var enumValue = ParseEnumValue(valueBytes);
                values.Add(enumValue);
            }
        }

        return values;
    }

    private static List<ProtobufEnumDescriptor> CollectFileEnums(IReadOnlyList<ProtobufField> fields, string packageFullName)
    {
        var enums = new List<ProtobufEnumDescriptor>();
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == 5 && field.Value is byte[] enumBytes)
            {
                var enumDescriptor = ParseEnum(enumBytes, packageFullName);
                enums.Add(enumDescriptor);
            }
        }

        return enums;
    }

    private static List<ProtobufMessageDescriptor> CollectFileMessages(IReadOnlyList<ProtobufField> fields, string packageFullName)
    {
        var messages = new List<ProtobufMessageDescriptor>();
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == 4 && field.Value is byte[] messageBytes)
            {
                var message = ParseMessage(messageBytes, packageFullName);
                messages.Add(message);
            }
        }

        return messages;
    }

    private static List<ProtobufServiceDescriptor> CollectFileServices(IReadOnlyList<ProtobufField> fields, string package)
    {
        var services = new List<ProtobufServiceDescriptor>();
        var packageFullName = string.IsNullOrEmpty(package) ? string.Empty : "." + package;
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == 6 && field.Value is byte[] serviceBytes)
            {
                var service = ParseService(serviceBytes, packageFullName, package);
                services.Add(service);
            }
        }

        return services;
    }

    private static List<ProtobufFieldDescriptor> CollectMessageFields(IReadOnlyList<ProtobufField> fields)
    {
        var fieldDescriptors = new List<ProtobufFieldDescriptor>();
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == 2 && field.Value is byte[] fieldBytes)
            {
                var fieldDescriptor = ParseField(fieldBytes);
                fieldDescriptors.Add(fieldDescriptor);
            }
        }

        return fieldDescriptors;
    }

    private static List<ProtobufEnumDescriptor> CollectMessageNestedEnums(IReadOnlyList<ProtobufField> fields, string parentFullName)
    {
        var nestedEnums = new List<ProtobufEnumDescriptor>();
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == 4 && field.Value is byte[] enumBytes)
            {
                var nestedEnum = ParseEnum(enumBytes, parentFullName);
                nestedEnums.Add(nestedEnum);
            }
        }

        return nestedEnums;
    }

    private static List<ProtobufMessageDescriptor> CollectMessageNestedMessages(IReadOnlyList<ProtobufField> fields, string parentFullName)
    {
        var nestedMessages = new List<ProtobufMessageDescriptor>();
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == 3 && field.Value is byte[] messageBytes)
            {
                var nestedMessage = ParseMessage(messageBytes, parentFullName);
                nestedMessages.Add(nestedMessage);
            }
        }

        return nestedMessages;
    }

    private static List<ProtobufMethodDescriptor> CollectServiceMethods(IReadOnlyList<ProtobufField> fields, string parentPackage, string serviceName)
    {
        var methods = new List<ProtobufMethodDescriptor>();
        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (field.FieldNumber == 2 && field.Value is byte[] methodBytes)
            {
                var method = ParseMethod(methodBytes, parentPackage, serviceName);
                methods.Add(method);
            }
        }

        return methods;
    }

    private static ProtobufEnumDescriptor ParseEnum(byte[] bytes, string parentFullName)
    {
        var fields = ProtobufDecoder.Decode(bytes);
        var name = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 1);
        var values = CollectEnumValues(fields);
        var enumDescriptor = new ProtobufEnumDescriptor
        {
            FullName = ProtobufFileDescriptorSetParserHelpers.BuildFullName(parentFullName, name),
            Name = name,
            Values = values,
        };
        return enumDescriptor;
    }

    private static ProtobufEnumValueDescriptor ParseEnumValue(byte[] bytes)
    {
        var fields = ProtobufDecoder.Decode(bytes);
        var name = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 1);
        ProtobufFileDescriptorSetParserHelpers.HasInt32Field(fields, fieldNumber: 2, out var number);
        var enumValue = new ProtobufEnumValueDescriptor
        {
            Name = name,
            Number = number,
        };
        return enumValue;
    }

    private static ProtobufFieldDescriptor ParseField(byte[] bytes)
    {
        var fields = ProtobufDecoder.Decode(bytes);
        var name = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 1);
        ProtobufFileDescriptorSetParserHelpers.HasInt32Field(fields, fieldNumber: 3, out var number);
        var label = ProtobufFieldLabel.Optional;
        if (ProtobufFileDescriptorSetParserHelpers.HasInt32Field(fields, fieldNumber: 4, out var rawLabel))
        {
            label = (ProtobufFieldLabel)rawLabel;
        }

        var kind = ProtobufFieldKind.TypeString;
        if (ProtobufFileDescriptorSetParserHelpers.HasInt32Field(fields, fieldNumber: 5, out var rawKind))
        {
            kind = (ProtobufFieldKind)rawKind;
        }

        var typeName = ProtobufFileDescriptorSetParserHelpers.TryReadStringField(fields, fieldNumber: 6);
        var fieldDescriptor = new ProtobufFieldDescriptor
        {
            Kind = kind,
            Label = label,
            Name = name,
            Number = number,
            TypeName = typeName,
        };
        return fieldDescriptor;
    }

    private static ProtobufFileDescriptor ParseFile(byte[] bytes)
    {
        var fields = ProtobufDecoder.Decode(bytes);
        var name = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 1);
        var package = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 2);
        var packageFullName = string.IsNullOrEmpty(package) ? string.Empty : "." + package;
        var messages = CollectFileMessages(fields, packageFullName);
        var enums = CollectFileEnums(fields, packageFullName);
        var services = CollectFileServices(fields, package);
        var file = new ProtobufFileDescriptor
        {
            Enums = enums,
            Messages = messages,
            Name = name,
            Package = package,
            Services = services,
        };
        return file;
    }

    private static ProtobufMessageDescriptor ParseMessage(byte[] bytes, string parentFullName)
    {
        var fields = ProtobufDecoder.Decode(bytes);
        var name = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 1);
        var fullName = ProtobufFileDescriptorSetParserHelpers.BuildFullName(parentFullName, name);
        var fieldDescriptors = CollectMessageFields(fields);
        var nestedMessages = CollectMessageNestedMessages(fields, fullName);
        var nestedEnums = CollectMessageNestedEnums(fields, fullName);
        var messageDescriptor = new ProtobufMessageDescriptor
        {
            Fields = fieldDescriptors,
            FullName = fullName,
            Name = name,
            NestedEnums = nestedEnums,
            NestedMessages = nestedMessages,
        };
        return messageDescriptor;
    }

    private static ProtobufMethodDescriptor ParseMethod(byte[] bytes, string servicePackage, string serviceName)
    {
        var fields = ProtobufDecoder.Decode(bytes);
        var name = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 1);
        var inputType = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 2);
        var outputType = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 3);
        ProtobufFileDescriptorSetParserHelpers.HasBoolField(fields, fieldNumber: 5, out var isClientStreaming);
        ProtobufFileDescriptorSetParserHelpers.HasBoolField(fields, fieldNumber: 6, out var isServerStreaming);
        var fullPath = BuildMethodPath(servicePackage, serviceName, name);
        var method = new ProtobufMethodDescriptor
        {
            FullPath = fullPath,
            InputType = inputType,
            IsClientStreaming = isClientStreaming,
            IsServerStreaming = isServerStreaming,
            Name = name,
            OutputType = outputType,
        };
        return method;
    }

    private static ProtobufServiceDescriptor ParseService(byte[] bytes, string parentFullName, string parentPackage)
    {
        var fields = ProtobufDecoder.Decode(bytes);
        var name = ProtobufFileDescriptorSetParserHelpers.ReadStringField(fields, fieldNumber: 1);
        var fullName = ProtobufFileDescriptorSetParserHelpers.BuildFullName(parentFullName, name);
        var methods = CollectServiceMethods(fields, parentPackage, name);
        var service = new ProtobufServiceDescriptor
        {
            FullName = fullName,
            Methods = methods,
            Name = name,
        };
        return service;
    }
}
