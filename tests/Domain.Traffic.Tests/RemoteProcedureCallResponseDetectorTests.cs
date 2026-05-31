using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallResponseDetector" />.
/// </summary>
public sealed class RemoteProcedureCallResponseDetectorTests
{
    /// <summary>
    ///     The plain <c>application/grpc</c> content type is detected.
    /// </summary>
    [Test]
    public async Task HasRemoteProcedureCallResponse_PlainGrpcContentType_ReturnsTrue()
    {
        var headers = BuildHeaders("Content-Type", "application/grpc");

        var hasMatch = RemoteProcedureCallResponseDetector.HasRemoteProcedureCallResponse(headers);

        await Assert.That(hasMatch).IsTrue();
    }

    /// <summary>
    ///     The <c>application/grpc+proto</c> subtype is detected.
    /// </summary>
    [Test]
    public async Task HasRemoteProcedureCallResponse_GrpcProtoSubtype_ReturnsTrue()
    {
        var headers = BuildHeaders("Content-Type", "application/grpc+proto");

        var hasMatch = RemoteProcedureCallResponseDetector.HasRemoteProcedureCallResponse(headers);

        await Assert.That(hasMatch).IsTrue();
    }

    /// <summary>
    ///     Case differences in the content type value are ignored.
    /// </summary>
    [Test]
    public async Task HasRemoteProcedureCallResponse_MixedCaseContentType_ReturnsTrue()
    {
        var headers = BuildHeaders("Content-Type", "APPLICATION/grpc+json");

        var hasMatch = RemoteProcedureCallResponseDetector.HasRemoteProcedureCallResponse(headers);

        await Assert.That(hasMatch).IsTrue();
    }

    /// <summary>
    ///     Case differences in the header name are ignored.
    /// </summary>
    [Test]
    public async Task HasRemoteProcedureCallResponse_LowerCaseHeaderName_ReturnsTrue()
    {
        var headers = BuildHeaders("content-type", "application/grpc-web");

        var hasMatch = RemoteProcedureCallResponseDetector.HasRemoteProcedureCallResponse(headers);

        await Assert.That(hasMatch).IsTrue();
    }

    /// <summary>
    ///     Non-gRPC content types are not detected.
    /// </summary>
    [Test]
    public async Task HasRemoteProcedureCallResponse_JsonContentType_ReturnsFalse()
    {
        var headers = BuildHeaders("Content-Type", "application/json");

        var hasMatch = RemoteProcedureCallResponseDetector.HasRemoteProcedureCallResponse(headers);

        await Assert.That(hasMatch).IsFalse();
    }

    /// <summary>
    ///     Empty header collections are not detected.
    /// </summary>
    [Test]
    public async Task HasRemoteProcedureCallResponse_NoContentType_ReturnsFalse()
    {
        var headers = HeaderCollection.Empty;

        var hasMatch = RemoteProcedureCallResponseDetector.HasRemoteProcedureCallResponse(headers);

        await Assert.That(hasMatch).IsFalse();
    }

    /// <summary>
    ///     An empty content type value is not detected.
    /// </summary>
    [Test]
    public async Task HasRemoteProcedureCallResponse_EmptyContentType_ReturnsFalse()
    {
        var headers = BuildHeaders("Content-Type", string.Empty);

        var hasMatch = RemoteProcedureCallResponseDetector.HasRemoteProcedureCallResponse(headers);

        await Assert.That(hasMatch).IsFalse();
    }

    private static HeaderCollection BuildHeaders(string name, string value)
    {
        var headers = HeaderCollection.Empty.Add(name, value);
        return headers;
    }
}
