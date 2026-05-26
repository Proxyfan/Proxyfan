using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Tests;

/// <summary>
///     Tests for <see cref="SessionError" />.
/// </summary>
public sealed class SessionErrorTests
{
    /// <summary>
    ///     Verifies that the constructor stores the code and message.
    /// </summary>
    [Test]
    public async Task Constructor_WithValues_StoresCodeAndMessage()
    {
        var error = new SessionError("SESSION_EXPORT_FAILED", "Failed to export session.");

        await Assert.That(error.Code).IsEqualTo("SESSION_EXPORT_FAILED");
        await Assert.That(error.Message).IsEqualTo("Failed to export session.");
    }
}
