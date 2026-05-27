using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallStatusParser" />.
/// </summary>
public sealed class RemoteProcedureCallStatusParserTests
{
    /// <summary>
    ///     Verifies that a numeric grpc-status with a message parses both fields.
    /// </summary>
    [Test]
    public async Task Parse_StatusAndMessage_ParsesBoth()
    {
        var status = RemoteProcedureCallStatusParser.Parse("5", "Not found");

        await Assert.That(status).IsNotNull();
        await Assert.That(status!.RawCode).IsEqualTo(5);
        await Assert.That(status.Code).IsEqualTo(RemoteProcedureCallStatusCode.NotFound);
        await Assert.That(status.Message).IsEqualTo("Not found");
    }

    /// <summary>
    ///     Verifies that an unknown numeric code maps to Unknown but preserves RawCode.
    /// </summary>
    [Test]
    public async Task Parse_UnknownNumericCode_MapsToUnknown()
    {
        var status = RemoteProcedureCallStatusParser.Parse("42", null);

        await Assert.That(status!.RawCode).IsEqualTo(42);
        await Assert.That(status.Code).IsEqualTo(RemoteProcedureCallStatusCode.Unknown);
    }

    /// <summary>
    ///     Verifies that an absent grpc-status returns null.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Parse_AbsentStatus_ReturnsNull(string? value)
    {
        var status = RemoteProcedureCallStatusParser.Parse(value, "x");

        await Assert.That(status).IsNull();
    }

    /// <summary>
    ///     Verifies that a non-numeric grpc-status returns null.
    /// </summary>
    [Test]
    public async Task Parse_NonNumericStatus_ReturnsNull()
    {
        var status = RemoteProcedureCallStatusParser.Parse("not-a-number", null);

        await Assert.That(status).IsNull();
    }

    /// <summary>
    ///     Verifies that an absent grpc-message yields a null Message.
    /// </summary>
    [Test]
    public async Task Parse_AbsentMessage_LeavesMessageNull()
    {
        var status = RemoteProcedureCallStatusParser.Parse("0", null);

        await Assert.That(status!.Message).IsNull();
    }

    /// <summary>
    ///     Verifies that OK (status 0) maps correctly.
    /// </summary>
    [Test]
    public async Task Parse_StatusZero_MapsToOk()
    {
        var status = RemoteProcedureCallStatusParser.Parse("0", null);

        await Assert.That(status!.Code).IsEqualTo(RemoteProcedureCallStatusCode.Ok);
    }

    /// <summary>
    ///     Verifies that Unauthenticated (16) maps correctly.
    /// </summary>
    [Test]
    public async Task Parse_StatusSixteen_MapsToUnauthenticated()
    {
        var status = RemoteProcedureCallStatusParser.Parse("16", null);

        await Assert.That(status!.Code).IsEqualTo(RemoteProcedureCallStatusCode.Unauthenticated);
    }
}
