using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Indexes a set of <see cref="ProtobufFileDescriptor" /> instances for fast lookup by
///     fully qualified message/enum name and by gRPC method path. Built once from a parsed
///     FileDescriptorSet and held immutable thereafter.
/// </summary>
public sealed class ProtobufDescriptorIndex
{
    private readonly Dictionary<string, ProtobufEnumDescriptor> _enumsByFullName;
    private readonly Dictionary<string, ProtobufMessageDescriptor> _messagesByFullName;
    private readonly Dictionary<string, ProtobufMethodDescriptor> _methodsByPath;

    /// <summary>
    ///     Gets the underlying file descriptors that were indexed.
    /// </summary>
    public IReadOnlyList<ProtobufFileDescriptor> Files { get; }

    /// <summary>
    ///     Initializes a new <see cref="ProtobufDescriptorIndex" /> by walking the supplied
    ///     file descriptors and populating the lookup tables.
    /// </summary>
    /// <param name="files">The file descriptors to index.</param>
    public ProtobufDescriptorIndex(IReadOnlyList<ProtobufFileDescriptor> files)
    {
        Files = files;
        var messagesByFullName = new Dictionary<string, ProtobufMessageDescriptor>();
        _messagesByFullName = messagesByFullName;
        var enumsByFullName = new Dictionary<string, ProtobufEnumDescriptor>();
        _enumsByFullName = enumsByFullName;
        var methodsByPath = new Dictionary<string, ProtobufMethodDescriptor>();
        _methodsByPath = methodsByPath;

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            IndexFile(file);
        }
    }

    /// <summary>
    ///     Looks up an enum by its fully qualified name (with the leading dot included).
    /// </summary>
    /// <param name="fullName">The fully qualified name, e.g. <c>".foo.Color"</c>.</param>
    /// <returns>The matched enum descriptor, or <see langword="null" /> when not found.</returns>
    public ProtobufEnumDescriptor? TryResolveEnum(string fullName)
    {
        if (_enumsByFullName.TryGetValue(fullName, out var enumDescriptor))
        {
            return enumDescriptor;
        }

        return null;
    }

    /// <summary>
    ///     Looks up a message by its fully qualified name (with the leading dot included).
    /// </summary>
    /// <param name="fullName">The fully qualified name, e.g. <c>".foo.HelloRequest"</c>.</param>
    /// <returns>The matched message descriptor, or <see langword="null" /> when not found.</returns>
    public ProtobufMessageDescriptor? TryResolveMessage(string fullName)
    {
        if (_messagesByFullName.TryGetValue(fullName, out var messageDescriptor))
        {
            return messageDescriptor;
        }

        return null;
    }

    /// <summary>
    ///     Looks up a gRPC method by its wire path (<c>/package.Service/Method</c>).
    /// </summary>
    /// <param name="path">The HTTP/2 <c>:path</c> header value.</param>
    /// <returns>The matched method descriptor, or <see langword="null" /> when not found.</returns>
    public ProtobufMethodDescriptor? TryResolveMethod(string path)
    {
        if (_methodsByPath.TryGetValue(path, out var methodDescriptor))
        {
            return methodDescriptor;
        }

        return null;
    }

    private void IndexEnum(ProtobufEnumDescriptor enumDescriptor)
    {
        _enumsByFullName[enumDescriptor.FullName] = enumDescriptor;
    }

    private void IndexFile(ProtobufFileDescriptor file)
    {
        for (var index = 0; index < file.Messages.Count; index++)
        {
            IndexMessage(file.Messages[index]);
        }

        for (var index = 0; index < file.Enums.Count; index++)
        {
            IndexEnum(file.Enums[index]);
        }

        for (var index = 0; index < file.Services.Count; index++)
        {
            var service = file.Services[index];
            for (var methodIndex = 0; methodIndex < service.Methods.Count; methodIndex++)
            {
                var method = service.Methods[methodIndex];
                _methodsByPath[method.FullPath] = method;
            }
        }
    }

    private void IndexMessage(ProtobufMessageDescriptor message)
    {
        _messagesByFullName[message.FullName] = message;
        for (var index = 0; index < message.NestedMessages.Count; index++)
        {
            IndexMessage(message.NestedMessages[index]);
        }

        for (var index = 0; index < message.NestedEnums.Count; index++)
        {
            IndexEnum(message.NestedEnums[index]);
        }
    }
}
